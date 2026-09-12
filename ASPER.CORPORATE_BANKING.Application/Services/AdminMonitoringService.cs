using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ASPER.CORPORATE_BANKING.Application.DTOs;
using ASPER.CORPORATE_BANKING.Application.Interfaces;
using ASPER.CORPORATE_BANKING.Infrastructure.Data;

namespace ASPER.CORPORATE_BANKING.Application.Services
{
    public class AdminMonitoringService : IAdminMonitoringService
    {
        private readonly CORPORATE_BANKINGDbContext _context;

        public AdminMonitoringService(CORPORATE_BANKINGDbContext context)
        {
            _context = context;
        }

        public async Task<ResultDto> GetPendingTransactionsAsync()
        {
            var stuckRecords = await _context.TransactionRecords
                .Include(r => r.matrixSlab)
                .Where(r => r.status == "PendingCheck" || r.status == "PendingApproval")
                .ToListAsync();

            if (!stuckRecords.Any())
            {
                return ResultDto.Success("No stuck transactions found.") is ResultDto r1 
                       ? new ResultDto { IsSuccess = r1.IsSuccess, Message = r1.Message, Data = new List<TransactionMonitoringResult>() } 
                       : ResultDto.Success();
            }

            // We need to fetch step roles for PendingApproval
            var stepConfigs = await _context.ApprovalSteps
                .Where(s => stuckRecords.Select(r => r.matrixSlabId).Contains(s.approvalMatrixSlabId))
                .ToListAsync();

            var stepIds = stepConfigs.Select(s => s.Id).ToList();
            var allStepRoles = await _context.ApprovalStepRoles
                .Where(sr => stepIds.Contains(sr.approvalStepId ?? 0))
                .ToListAsync();

            var results = new List<TransactionMonitoringResult>();

            foreach (var record in stuckRecords)
            {
                var result = new TransactionMonitoringResult
                {
                    TransactionRecordId = record.Id,
                    InstructionRefNo = record.instructionRefNo,
                    Amount = record.amount ?? 0,
                    Status = record.status,
                    LastActionAt = record.updatedAt ?? record.createdAt
                };

                if (result.LastActionAt.HasValue)
                {
                    result.AgingInMinutes = (DateTime.UtcNow - result.LastActionAt.Value).TotalMinutes;
                }

                if (record.status == "PendingCheck" && record.matrixSlab != null)
                {
                    result.CurrentlyPendingWithRoles = record.matrixSlab.checkerRoleName ?? "Checker";
                }
                else if (record.status == "PendingApproval" && record.currentStepOrder.HasValue && record.matrixSlabId.HasValue)
                {
                    var currentStep = stepConfigs.FirstOrDefault(s => s.approvalMatrixSlabId == record.matrixSlabId.Value && s.stepOrder == record.currentStepOrder.Value);
                    if (currentStep != null)
                    {
                        var stepRoles = allStepRoles.Where(sr => sr.approvalStepId == currentStep.Id).ToList();
                        if (stepRoles.Any())
                        {
                            var roleNames = stepRoles.Select(sr => sr.roleName).ToList();
                            result.CurrentlyPendingWithRoles = string.Join(" OR ", roleNames);
                        }
                        else
                        {
                            result.CurrentlyPendingWithRoles = "Unknown Approver";
                        }
                    }
                    else
                    {
                        result.CurrentlyPendingWithRoles = "Unknown Approver";
                    }
                }

                results.Add(result);
            }

            return ResultDto.Success("Fetched pending transactions.") is ResultDto res 
                   ? new ResultDto { IsSuccess = res.IsSuccess, Message = res.Message, Data = results.OrderByDescending(x => x.AgingInMinutes).ToList() } 
                   : ResultDto.Success();
        }

        public async Task<ResultDto> GetAllBatchesAsync()
        {
            var batches = await _context.TransactionBatches
                .OrderByDescending(b => b.uploadedAt)
                .Select(b => new 
                {
                    b.Id,
                    b.batchNo,
                    b.fileName,
                    b.uploadedAt,
                    b.uploadedByUserName,
                    b.totalRecords,
                    b.validRecords,
                    b.invalidRecords,
                    b.totalAmount,
                    b.status
                })
                .ToListAsync();

            return ResultDto.Success("Fetched all batches.") is ResultDto res
                   ? new ResultDto { IsSuccess = res.IsSuccess, Message = res.Message, Data = batches }
                   : ResultDto.Success();
        }

        public async Task<ResultDto> GetTransactionsAsync(string status, int? typeId, DateTime? dateFrom, DateTime? dateTo, decimal? minAmount, decimal? maxAmount)
        {
            var query = _context.TransactionRecords
                .Include(r => r.matrixSlab)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.status == status);
            
            if (dateFrom.HasValue)
                query = query.Where(r => r.createdAt >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(r => r.createdAt <= dateTo.Value);

            if (minAmount.HasValue)
                query = query.Where(r => r.amount >= minAmount.Value);

            if (maxAmount.HasValue)
                query = query.Where(r => r.amount <= maxAmount.Value);

            var records = await query.OrderByDescending(r => r.createdAt).Take(500).ToListAsync();

            var results = new List<TransactionMonitoringResult>();
            foreach (var record in records)
            {
                var result = new TransactionMonitoringResult
                {
                    TransactionRecordId = record.Id,
                    InstructionRefNo = record.instructionRefNo,
                    Amount = record.amount ?? 0,
                    Status = record.status,
                    LastActionAt = record.updatedAt ?? record.createdAt
                };

                if (result.LastActionAt.HasValue)
                {
                    result.AgingInMinutes = (DateTime.UtcNow - result.LastActionAt.Value).TotalMinutes;
                }

                if (record.status == "PendingCheck" && record.matrixSlab != null)
                {
                    result.CurrentlyPendingWithRoles = record.matrixSlab.checkerRoleName ?? "Checker";
                }
                else if (record.status == "PendingApproval")
                {
                    result.CurrentlyPendingWithRoles = "Approver(s)";
                }

                results.Add(result);
            }

            return ResultDto.Success("Fetched transactions.") is ResultDto res 
                   ? new ResultDto { IsSuccess = res.IsSuccess, Message = res.Message, Data = results } 
                   : ResultDto.Success();
        }

        public async Task<ResultDto> GetTransactionAuditAsync(int transactionRecordId)
        {
            var audits = await _context.TransactionApprovalActions
                .Where(a => a.transactionRecordId == transactionRecordId)
                .OrderByDescending(a => a.actionAt)
                .Select(a => new TransactionAuditResult
                {
                    ActionId = a.Id,
                    ActionType = a.actionType,
                    ActionByUserId = a.actionByUserId,
                    ActionByRoleId = a.actionByRoleId,
                    Remarks = a.remarks,
                    ActionAt = a.actionAt ?? DateTime.MinValue
                })
                .ToListAsync();

            return ResultDto.Success("Fetched audit trail.") is ResultDto res
                   ? new ResultDto { IsSuccess = res.IsSuccess, Message = res.Message, Data = audits }
                   : ResultDto.Success();
        }

        public async Task<ResultDto> GetTransactionDetailAsync(int transactionRecordId)
        {
            var record = await _context.TransactionRecords
                .Include(r => r.transactionBatch)
                .Where(r => r.Id == transactionRecordId)
                .Select(r => new {
                    r.Id,
                    r.instructionRefNo,
                    r.amount,
                    r.currency,
                    r.bankAccountNo,
                    r.accountHolderName,
                    r.routingNumber,
                    r.senderAccountNo,
                    r.senderAccountName,
                    r.purposeCode,
                    r.status,
                    r.currentStepOrder,
                    totalSteps = _context.ApprovalSteps.Count(s => s.approvalMatrixSlabId == r.matrixSlabId),
                    batchNo = r.transactionBatch != null ? (Guid?)r.transactionBatch.batchNo : null,
                    uploadedAt = r.transactionBatch != null ? (DateTime?)r.transactionBatch.uploadedAt : null
                })
                .FirstOrDefaultAsync();

            if (record == null)
            {
                return ResultDto.Failure("Transaction not found.");
            }

            return ResultDto.Success("Fetched transaction detail.") is ResultDto res 
                   ? new ResultDto { IsSuccess = res.IsSuccess, Message = res.Message, Data = record } 
                   : ResultDto.Success();
        }
    }
}



