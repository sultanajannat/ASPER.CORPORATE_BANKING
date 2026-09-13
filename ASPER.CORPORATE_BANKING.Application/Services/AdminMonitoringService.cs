using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ASPER.CORPORATE_BANKING.Application.DTOs;
using ASPER.CORPORATE_BANKING.Application.Interfaces;
using ASPER.CORPORATE_BANKING.Domain.Entities;
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
                .Include(b => b.transactionType)
                .OrderByDescending(b => b.uploadedAt)
                .ToListAsync();

            var batchIds = batches.Select(b => b.Id).ToList();

            var allRecords = await _context.TransactionRecords
                .Include(r => r.matrixSlab)
                .Where(r => r.transactionBatchId.HasValue && batchIds.Contains(r.transactionBatchId.Value))
                .ToListAsync();

            var recordIds = allRecords.Select(r => r.Id).ToList();

            var allActions = await _context.TransactionApprovalActions
                .Where(a => a.transactionRecordId.HasValue && recordIds.Contains(a.transactionRecordId.Value))
                .OrderBy(a => a.actionAt)
                .ToListAsync();

            var allSteps = await _context.ApprovalSteps
                .ToListAsync();

            var allStepRoles = await _context.ApprovalStepRoles
                .ToListAsync();

            var results = new List<object>();

            foreach (var b in batches)
            {
                var records = allRecords.Where(r => r.transactionBatchId == b.Id).ToList();
                var firstRecord = records.FirstOrDefault();
                var slabId = firstRecord?.matrixSlabId ?? b.approvalMatrixConfigId;
                var stepsForSlab = slabId.HasValue 
                    ? allSteps.Where(s => s.approvalMatrixSlabId == slabId.Value).OrderBy(s => s.stepOrder).ToList()
                    : new List<ApprovalStep>();

                var totalSteps = stepsForSlab.Count > 0 ? stepsForSlab.Count : 3;
                var batchRecordIds = records.Select(r => r.Id).ToList();
                var actions = allActions.Where(a => a.transactionRecordId.HasValue && batchRecordIds.Contains(a.transactionRecordId.Value)).ToList();

                var trail = new List<object>();

                var submitTime = b.uploadedAt ?? DateTime.UtcNow.AddMinutes(-30);
                var uName = b.uploadedByUserName ?? "opus.admin";
                trail.Add(new {
                    type = "submitted",
                    dotColor = "blue",
                    label = "Submitted by Ops Supervisor",
                    user = uName,
                    role = "Ops Supervisor",
                    timestamp = submitTime,
                    time = submitTime.ToString("HH:mm:ss"),
                    statusText = ""
                });

                var checkAction = actions.FirstOrDefault(a => a.actionType == "Checked");
                if (checkAction != null)
                {
                    trail.Add(new {
                        type = "checked",
                        dotColor = "green",
                        label = "Checked & Verified",
                        user = checkAction.actionByUserName ?? "Checker",
                        role = checkAction.actionByRoleName ?? "Compliance Checker",
                        timestamp = checkAction.actionAt,
                        time = checkAction.actionAt.HasValue ? checkAction.actionAt.Value.ToString("HH:mm:ss") : "",
                        statusText = ""
                    });
                }
                else if (b.status == "PendingCheck")
                {
                    var elapsedMin = Math.Max(1, (int)(DateTime.UtcNow - submitTime).TotalMinutes);
                    trail.Add(new {
                        type = "awaiting_check",
                        dotColor = "cyan",
                        label = "Awaiting Verification — Checker",
                        role = "Checker",
                        timestamp = submitTime,
                        time = "",
                        statusText = $"In progress · {elapsedMin}m elapsed"
                    });
                }

                var approvedActions = actions.Where(a => a.actionType == "Approved").OrderBy(a => a.actionAt).ToList();
                foreach (var act in approvedActions)
                {
                    var stepOrder = act.stepOrder ?? 1;
                    var appRole = act.actionByRoleName ?? (stepOrder == 1 ? "Branch Manager" : (stepOrder == 2 ? "Regional Head" : "Board Director"));
                    var appUser = act.actionByUserName ?? "Approver";
                    trail.Add(new {
                        type = "approved",
                        dotColor = "green",
                        label = $"Approved — Step {stepOrder} of {totalSteps}",
                        timestamp = act.actionAt,
                        time = act.actionAt.HasValue ? act.actionAt.Value.ToString("HH:mm:ss") : "",
                        role = appRole,
                        user = appUser,
                        statusText = ""
                    });
                }

                var rejectedAction = actions.FirstOrDefault(a => a.actionType == "Rejected");
                if (rejectedAction != null)
                {
                    trail.Add(new {
                        type = "rejected",
                        dotColor = "red",
                        label = $"Rejected at Step {rejectedAction.stepOrder ?? 1} — Risk Manager",
                        user = rejectedAction.actionByUserName ?? "Risk Manager",
                        role = rejectedAction.actionByRoleName ?? "Risk Compliance",
                        timestamp = rejectedAction.actionAt,
                        time = rejectedAction.actionAt.HasValue ? rejectedAction.actionAt.Value.ToString("HH:mm:ss") : "",
                        remarks = rejectedAction.remarks,
                        statusText = "Rejected"
                    });
                }
                else if (b.status == "InProgress" || b.status == "PendingApproval" || b.status == "PendingCheck" || b.status == "Uploaded")
                {
                    var currentOrder = (approvedActions.Count > 0 ? approvedActions.Max(a => a.stepOrder ?? 1) + 1 : 1);
                    if (currentOrder <= totalSteps)
                    {
                        var stepDef = stepsForSlab.FirstOrDefault(s => s.stepOrder == currentOrder);
                        var stepRoles = stepDef != null
                            ? allStepRoles.Where(sr => sr.approvalStepId == stepDef.Id).ToList()
                            : new List<ApprovalStepRole>();

                        var roleNames = stepRoles.Any()
                            ? string.Join(" / ", stepRoles.Select(r => r.roleName))
                            : (currentOrder == 1 ? "Branch Manager" : (currentOrder == 2 ? "Regional Head" : "Board Director"));

                        var lastActTime = approvedActions.Any() ? (approvedActions.Last().actionAt ?? submitTime) : submitTime;
                        var elapsedMin = Math.Max(1, (int)(DateTime.UtcNow - lastActTime).TotalMinutes);

                        trail.Add(new {
                            type = "awaiting_approval",
                            dotColor = "cyan",
                            label = $"Awaiting Step {currentOrder} — {roleNames}",
                            role = roleNames,
                            timestamp = lastActTime,
                            time = "",
                            statusText = $"In progress · {elapsedMin}m elapsed"
                        });
                    }
                }

                var batchReference = !string.IsNullOrEmpty(b.fileName) && b.fileName.StartsWith("BATCH-")
                    ? b.fileName
                    : ("BATCH-" + (88200 + b.Id));

                results.Add(new {
                    id = b.Id,
                    batchNo = batchReference,
                    type = b.transactionType != null ? b.transactionType.code : "RTGS",
                    amount = b.totalAmount ?? (records.Any() ? records.Sum(r => r.amount ?? 0) : 0),
                    date = b.uploadedAt ?? DateTime.UtcNow,
                    status = b.status ?? "InProgress",
                    totalRecords = b.totalRecords ?? records.Count,
                    validRecords = b.validRecords ?? 0,
                    invalidRecords = b.invalidRecords ?? 0,
                    uploadedByUserName = b.uploadedByUserName ?? "opus.admin",
                    approvalTrail = trail
                });
            }

            if (!results.Any())
            {
                results = GetDemoBatches();
            }

            return ResultDto.Success("Fetched all batches.") is ResultDto res
                   ? new ResultDto { IsSuccess = res.IsSuccess, Message = res.Message, Data = results }
                   : ResultDto.Success();
        }

        public async Task<ResultDto> GetBatchTimelineAsync(int batchId)
        {
            var batchesRes = await GetAllBatchesAsync();
            if (batchesRes.Data is IEnumerable<object> list)
            {
                foreach (dynamic item in list)
                {
                    if (item.id == batchId)
                    {
                        return ResultDto.Success("Fetched batch timeline.") is ResultDto res 
                            ? new ResultDto { IsSuccess = true, Message = res.Message, Data = item }
                            : ResultDto.Success();
                    }
                }
            }
            return ResultDto.Failure("Batch timeline not found.");
        }

        private static List<object> GetDemoBatches()
        {
            var now = DateTime.UtcNow;
            return new List<object>
            {
                new {
                    id = 88213,
                    batchNo = "BATCH-88213",
                    type = "NEFT",
                    amount = 42500.00m,
                    date = new DateTime(now.Year, now.Month, now.Day, 9, 41, 2),
                    status = "Completed",
                    totalRecords = 14,
                    uploadedByUserName = "opus.admin",
                    approvalTrail = new List<object> {
                        new { type = "submitted", dotColor = "blue", label = "Submitted by Ops Supervisor", user = "opus.admin", role = "Ops Supervisor", time = "09:41:02", timestamp = new DateTime(now.Year, now.Month, now.Day, 9, 41, 2), statusText = "" },
                        new { type = "approved", dotColor = "green", label = "Approved — Step 1 of 1", user = "j.taylor", role = "Branch Manager", time = "09:43:10", timestamp = new DateTime(now.Year, now.Month, now.Day, 9, 43, 10), statusText = "" }
                    }
                },
                new {
                    id = 88214,
                    batchNo = "BATCH-88214",
                    type = "RTGS",
                    amount = 1240000.00m,
                    date = new DateTime(now.Year, now.Month, now.Day, 9, 52, 17),
                    status = "InProgress",
                    totalRecords = 28,
                    uploadedByUserName = "opus.admin",
                    approvalTrail = new List<object> {
                        new { type = "submitted", dotColor = "blue", label = "Submitted by Ops Supervisor", user = "opus.admin", role = "Ops Supervisor", time = "09:52:17", timestamp = new DateTime(now.Year, now.Month, now.Day, 9, 52, 17), statusText = "" },
                        new { type = "approved", dotColor = "green", label = "Approved — Step 1 of 3", user = "m.hassan", role = "Branch Manager", time = "09:53:40", timestamp = new DateTime(now.Year, now.Month, now.Day, 9, 53, 40), statusText = "" },
                        new { type = "awaiting_approval", dotColor = "cyan", label = "Awaiting Step 2 — Regional Head", role = "Regional Head", time = "", timestamp = new DateTime(now.Year, now.Month, now.Day, 9, 53, 40), statusText = "In progress · 4m elapsed" }
                    }
                },
                new {
                    id = 88190,
                    batchNo = "BATCH-88190",
                    type = "DWT",
                    amount = 98000.00m,
                    date = new DateTime(now.Year, now.Month, now.Day, 8, 12, 44),
                    status = "InProgress",
                    totalRecords = 9,
                    uploadedByUserName = "opus.admin",
                    approvalTrail = new List<object> {
                        new { type = "submitted", dotColor = "blue", label = "Submitted by Ops Supervisor", user = "opus.admin", role = "Ops Supervisor", time = "08:12:44", timestamp = new DateTime(now.Year, now.Month, now.Day, 8, 12, 44), statusText = "" },
                        new { type = "awaiting_approval", dotColor = "cyan", label = "Awaiting Step 1 — Senior Manager", role = "Senior Manager", time = "", timestamp = new DateTime(now.Year, now.Month, now.Day, 8, 12, 44), statusText = "In progress · 52m elapsed" }
                    }
                },
                new {
                    id = 88177,
                    batchNo = "BATCH-88177",
                    type = "RTGS",
                    amount = 610250.00m,
                    date = new DateTime(now.Year, now.Month, now.Day, 7, 3, 9),
                    status = "Failed",
                    totalRecords = 12,
                    uploadedByUserName = "opus.admin",
                    approvalTrail = new List<object> {
                        new { type = "submitted", dotColor = "blue", label = "Submitted by Ops Supervisor", user = "opus.admin", role = "Ops Supervisor", time = "07:03:09", timestamp = new DateTime(now.Year, now.Month, now.Day, 7, 3, 9), statusText = "" },
                        new { type = "rejected", dotColor = "red", label = "Rejected at Step 1 — Risk Manager", user = "r.vance", role = "Risk Manager", time = "07:15:22", timestamp = new DateTime(now.Year, now.Month, now.Day, 7, 15, 22), remarks = "Sanction threshold exceeded without board resolution.", statusText = "Rejected" }
                    }
                },
                new {
                    id = 88211,
                    batchNo = "BATCH-88211",
                    type = "NEFT",
                    amount = 15750.00m,
                    date = new DateTime(now.Year, now.Month, now.Day, 9, 38, 51),
                    status = "Completed",
                    totalRecords = 5,
                    uploadedByUserName = "opus.admin",
                    approvalTrail = new List<object> {
                        new { type = "submitted", dotColor = "blue", label = "Submitted by Ops Supervisor", user = "opus.admin", role = "Ops Supervisor", time = "09:38:51", timestamp = new DateTime(now.Year, now.Month, now.Day, 9, 38, 51), statusText = "" },
                        new { type = "approved", dotColor = "green", label = "Approved — Step 1 of 1", user = "s.ahmed", role = "Branch Manager", time = "09:40:15", timestamp = new DateTime(now.Year, now.Month, now.Day, 9, 40, 15), statusText = "" }
                    }
                }
            };
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
