using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using ASPER.CORPORATE_BANKING.Application.DTOs;
using ASPER.CORPORATE_BANKING.Application.Interfaces;
using ASPER.CORPORATE_BANKING.Domain.Entities;
using ASPER.CORPORATE_BANKING.Infrastructure.Audit.Outbox;
using ASPER.CORPORATE_BANKING.Infrastructure.Data;

namespace ASPER.CORPORATE_BANKING.Application.Services
{
    public class FileIngestionService : IFileIngestionService
    {
        private readonly CORPORATE_BANKINGDbContext _context;

        public FileIngestionService(CORPORATE_BANKINGDbContext context)
        {
            _context = context;
        }

        public async Task<ResultDto> IngestFileAsync(int transactionTypeId, Stream fileStream, string fileName, int uploadedByUserId, string uploadedByUserName)
        {
            var txType = await _context.TransactionTypes.FindAsync(transactionTypeId);
            if (txType == null) return ResultDto.Failure("Invalid Transaction Type.");

            var templateConfig = await _context.FileTemplateConfigs
                .FirstOrDefaultAsync(c => c.transactionTypeId == transactionTypeId && c.isActive);

            if (templateConfig == null) return ResultDto.Failure("Active File Template not found for this transaction type.");

            var mappings = await _context.FileFieldMappings
                .Where(m => m.fileTemplateConfigId == templateConfig.Id)
                .OrderBy(m => m.columnOrder)
                .ToListAsync();

            if (!mappings.Any()) return ResultDto.Failure("File Template has no column mappings configured.");

            var matrixConfig = await _context.ApprovalMatrixConfigs
                .FirstOrDefaultAsync(c => c.transactionTypeId == transactionTypeId && c.isActive);

            if (matrixConfig == null) return ResultDto.Failure("Active Approval Matrix Config not found.");

            var slabs = await _context.ApprovalMatrixSlabs
                .Where(s => s.approvalMatrixConfigId == matrixConfig.Id && s.isActive == true)
                .ToListAsync();

            if (!slabs.Any()) return ResultDto.Failure("Active Approval Matrix has no slabs configured.");

            var batch = new TransactionBatch
            {
                batchNo = Guid.NewGuid(),
                transactionTypeId = transactionTypeId,
                approvalMatrixConfigId = matrixConfig.Id,
                fileName = fileName,
                fileStoragePath = "Memory", // Or actual disk path if saved prior
                uploadedByUserId = uploadedByUserId,
                uploadedByUserName = uploadedByUserName,
                uploadedAt = DateTime.UtcNow,
                status = "InProgress"
            };

            var records = new List<TransactionRecord>();
            var errorMessages = new List<string>();

            int validCount = 0;
            int invalidCount = 0;
            decimal totalAmount = 0;

            try
            {
                using (var workbook = new XLWorkbook(fileStream))
                {
                    var worksheet = workbook.Worksheets.First();
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header

                    int rowIndex = 1; // 1-based data row index
                    foreach (var row in rows)
                    {
                        rowIndex++;
                        var record = new TransactionRecord { rowNo = rowIndex };
                        bool rowIsValid = true;

                        // Temporary dictionary to hold extracted values before strongly typing
                        var extractedValues = new Dictionary<string, string>();

                        foreach (var mapping in mappings)
                        {
                            var cellText = row.Cell(mapping.columnOrder ?? 1).GetString();

                            if (mapping.isMandatory == true && string.IsNullOrWhiteSpace(cellText))
                            {
                                errorMessages.Add($"Row {rowIndex}: {mapping.excelColumnName} is mandatory.");
                                rowIsValid = false;
                            }

                            if (!string.IsNullOrWhiteSpace(cellText) && mapping.maxLength.HasValue && cellText.Length > mapping.maxLength.Value)
                            {
                                errorMessages.Add($"Row {rowIndex}: {mapping.excelColumnName} exceeds maximum length {mapping.maxLength.Value}.");
                                rowIsValid = false;
                            }

                            if (!string.IsNullOrWhiteSpace(cellText) && !string.IsNullOrWhiteSpace(mapping.validationRegex))
                            {
                                if (!Regex.IsMatch(cellText, mapping.validationRegex))
                                {
                                    errorMessages.Add($"Row {rowIndex}: {mapping.excelColumnName} invalid format.");
                                    rowIsValid = false;
                                }
                            }

                            extractedValues[mapping.fieldKey] = cellText?.Trim();
                        }

                        if (!rowIsValid)
                        {
                            invalidCount++;
                            continue;
                        }

                        // Map extracted values to standard properties based on FieldKey convention
                        extractedValues.TryGetValue("INSTRUCTION_REF_NO", out string refNo);
                        extractedValues.TryGetValue("AMOUNT", out string amountStr);
                        extractedValues.TryGetValue("BANK_AC_NO", out string bankAcNo);
                        extractedValues.TryGetValue("AC_HOLDER_NAME", out string acHolderName);
                        extractedValues.TryGetValue("ROUTING_NO", out string routingNo);
                        extractedValues.TryGetValue("SENDER_AC_NO", out string senderAcNo);
                        extractedValues.TryGetValue("SENDER_AC_NAME", out string senderAcName);

                        if (string.IsNullOrWhiteSpace(refNo))
                        {
                            errorMessages.Add($"Row {rowIndex}: Missing or mapped INSTRUCTION_REF_NO.");
                            invalidCount++; continue;
                        }

                        // Local duplicate check
                        if (records.Any(r => r.instructionRefNo == refNo))
                        {
                            errorMessages.Add($"Row {rowIndex}: Duplicate InstructionRefNo '{refNo}' within the file.");
                            invalidCount++; continue;
                        }

                        // DB duplicate check
                        if (await _context.TransactionRecords.AnyAsync(r => r.instructionRefNo == refNo))
                        {
                            errorMessages.Add($"Row {rowIndex}: InstructionRefNo '{refNo}' already exists in the system.");
                            invalidCount++; continue;
                        }

                        if (!decimal.TryParse(amountStr, out decimal amount))
                        {
                            errorMessages.Add($"Row {rowIndex}: Invalid amount format.");
                            invalidCount++; continue;
                        }

                        if (amount <= 0)
                        {
                            errorMessages.Add($"Row {rowIndex}: Amount must be greater than zero.");
                            invalidCount++; continue;
                        }

                        record.instructionRefNo = refNo;
                        record.amount = amount;
                        record.bankAccountNo = bankAcNo;
                        record.accountHolderName = acHolderName;
                        record.routingNumber = routingNo;
                        record.senderAccountNo = senderAcNo;
                        record.senderAccountName = senderAcName;

                        // Slab Resolution
                        var matchingSlab = slabs.FirstOrDefault(s => amount >= s.minAmount && amount <= (s.maxAmount ?? decimal.MaxValue));
                        if (matchingSlab == null)
                        {
                            errorMessages.Add($"Row {rowIndex}: Amount {amount} does not fall into any active approval slab.");
                            invalidCount++; continue;
                        }

                        record.matrixSlabId = matchingSlab.Id;
                        record.checkerRequiredSnapshot = matchingSlab.isCheckerRequired ?? false;

                        if (matchingSlab.isAutoApprove == true)
                        {
                            record.status = "AutoApproved";
                            record.currentStepOrder = null;
                            
                            // Transactional Outbox Pattern: AutoApproved must immediately go to Outbox
                            var integrationEvent = new ApprovedTransactionIntegrationEvent
                            {
                                TransactionRecordId = record.Id, // Will be 0 here, but generated on save
                                InstructionRefNo = record.instructionRefNo,
                                Amount = record.amount ?? 0,
                                Currency = record.currency,
                                BeneficiaryAccount = record.bankAccountNo,
                                SenderAccount = record.senderAccountNo,
                                RoutingNumber = record.routingNumber,
                                PurposeCode = record.purposeCode
                            };

                            var outboxMessage = new AuditOutboxMessage
                            {
                                EventType = nameof(ApprovedTransactionIntegrationEvent),
                                Payload = JsonSerializer.Serialize(integrationEvent),
                                Status = "Pending",
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.AuditOutboxMessages.Add(outboxMessage);
                        }
                        else if (matchingSlab.isCheckerRequired == true)
                        {
                            record.status = "PendingCheck";
                            record.currentStepOrder = null;
                        }
                        else
                        {
                            record.status = "PendingApproval";
                            record.currentStepOrder = 1;
                        }

                        records.Add(record);
                        validCount++;
                        totalAmount += amount;
                    }
                }

                batch.totalRecords = validCount + invalidCount;
                batch.validRecords = validCount;
                batch.invalidRecords = invalidCount;
                batch.totalAmount = totalAmount;
                batch.validationErrors = errorMessages.Any() ? JsonSerializer.Serialize(errorMessages) : null;

                if (validCount > 0)
                {
                    batch.status = invalidCount > 0 ? "PartiallyCompleted" : "Completed"; // Or PendingCheck depending on business logic, sticking to PartiallyCompleted/Completed for Batch status. Let's use Uploaded/PartiallyCompleted
                    
                    _context.TransactionBatches.Add(batch);
                    
                    foreach (var r in records)
                    {
                        r.transactionBatch = batch;
                        _context.TransactionRecords.Add(r);
                    }

                    await _context.SaveChangesAsync();
                }

                var response = new UploadBatchResponse
                {
                    BatchNo = batch.batchNo,
                    TotalRecords = batch.totalRecords.Value,
                    ValidRecords = validCount,
                    InvalidRecords = invalidCount,
                    ErrorMessages = errorMessages
                };

                return ResultDto.Success("File processed.") is ResultDto res ? new ResultDto { IsSuccess = res.IsSuccess, Message = res.Message, Data = response } : ResultDto.Success();
            }
            catch (Exception ex)
            {
                return ResultDto.Failure($"Failed to process Excel file: {ex.Message}");
            }
        }

        public async Task<ResultDto> GetMakerBatchesAsync(string uploadedByUserName)
        {
            var batches = await _context.TransactionBatches
                .Where(b => b.uploadedByUserName == uploadedByUserName)
                .OrderByDescending(b => b.uploadedAt)
                .Select(b => new 
                {
                    b.Id,
                    b.batchNo,
                    b.fileName,
                    b.uploadedAt,
                    b.totalRecords,
                    b.validRecords,
                    b.invalidRecords,
                    b.totalAmount,
                    b.status
                })
                .ToListAsync();

            return ResultDto.Success("Fetched batches.") is ResultDto res ? new ResultDto { IsSuccess = res.IsSuccess, Message = res.Message, Data = batches } : ResultDto.Success();
        }

        public async Task<ResultDto> GetBatchValidationReportAsync(int batchId)
        {
            var batch = await _context.TransactionBatches
                .Where(b => b.Id == batchId)
                .Select(b => new { b.validationErrors })
                .FirstOrDefaultAsync();

            if (batch == null) return ResultDto.Failure("Batch not found.");

            var errors = string.IsNullOrEmpty(batch.validationErrors) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(batch.validationErrors);

            return ResultDto.Success("Fetched validation report.") is ResultDto res ? new ResultDto { IsSuccess = res.IsSuccess, Message = res.Message, Data = errors } : ResultDto.Success();
        }
    }
}
