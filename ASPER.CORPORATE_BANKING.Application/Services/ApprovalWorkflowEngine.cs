using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ASPER.CORPORATE_BANKING.Application.Interfaces;
using ASPER.CORPORATE_BANKING.Application.DTOs;
using ASPER.CORPORATE_BANKING.Infrastructure.Data;
using ASPER.CORPORATE_BANKING.Domain.Entities;
using System.Text.Json;
using ASPER.CORPORATE_BANKING.Infrastructure.Audit.Outbox;

namespace ASPER.CORPORATE_BANKING.Application.Services
{
    public class ApprovalWorkflowEngine : IApprovalWorkflowEngine
    {
        private readonly CORPORATE_BANKINGDbContext _context;

        public ApprovalWorkflowEngine(CORPORATE_BANKINGDbContext context)
        {
            _context = context;
        }

        private static List<string> ParseRoles(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return new List<string>();

            return roleName
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrEmpty(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string FindMatchingRole(IEnumerable<string> userRoles, string targetRole)
        {
            if (userRoles == null || string.IsNullOrWhiteSpace(targetRole))
                return null;

            return userRoles.FirstOrDefault(ur => string.Equals(ur.Trim(), targetRole.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public async Task<ResultDto> CheckAndForward(int transactionRecordId, string userName, string roleName, string remarks)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var record = await _context.TransactionRecords
                    .Include(r => r.matrixSlab)
                    .FirstOrDefaultAsync(r => r.Id == transactionRecordId);

                if (record == null) return ResultDto.Failure("Transaction record not found.");
                if (record.status != "PendingCheck") return ResultDto.Failure("Transaction is not in PendingCheck state.");

                // Validate if user has the specific checker role required for this slab (handling multiple user roles)
                var userRoles = ParseRoles(roleName);
                var requiredCheckerRole = record.matrixSlab?.checkerRoleName?.Trim();
                var matchedCheckerRole = FindMatchingRole(userRoles, requiredCheckerRole);

                if (string.IsNullOrEmpty(matchedCheckerRole))
                {
                    return ResultDto.Failure($"User role is not authorized to check this transaction. Required role: '{requiredCheckerRole}'. User assigned roles: '{roleName}'.");
                }

                var action = new TransactionApprovalAction
                {
                    transactionRecordId = record.Id,
                    actionType = "Checked",
                    actionByUserName = userName,
                    actionByRoleName = matchedCheckerRole,
                    actionAt = DateTime.UtcNow,
                    remarks = remarks
                };
                
                _context.TransactionApprovalActions.Add(action);

                record.status = "PendingApproval";
                record.currentStepOrder = 1;
                record.updatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResultDto.Success("Transaction checked and forwarded successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResultDto.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<ResultDto> CheckReject(int transactionRecordId, string userName, string roleName, string remarks)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var record = await _context.TransactionRecords
                    .Include(r => r.matrixSlab)
                    .FirstOrDefaultAsync(r => r.Id == transactionRecordId);

                if (record == null) return ResultDto.Failure("Transaction record not found.");
                if (record.status != "PendingCheck") return ResultDto.Failure("Transaction is not in PendingCheck state.");

                // Validate if user has the specific checker role required for this slab (handling multiple user roles)
                var userRoles = ParseRoles(roleName);
                var requiredCheckerRole = record.matrixSlab?.checkerRoleName?.Trim();
                var matchedCheckerRole = FindMatchingRole(userRoles, requiredCheckerRole);

                if (string.IsNullOrEmpty(matchedCheckerRole))
                {
                    return ResultDto.Failure($"User role is not authorized to check this transaction. Required role: '{requiredCheckerRole}'. User assigned roles: '{roleName}'.");
                }

                var action = new TransactionApprovalAction
                {
                    transactionRecordId = record.Id,
                    actionType = "Rejected",
                    actionByUserName = userName,
                    actionByRoleName = matchedCheckerRole,
                    actionAt = DateTime.UtcNow,
                    remarks = remarks
                };
                
                _context.TransactionApprovalActions.Add(action);

                record.status = "Rejected";
                record.updatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResultDto.Success("Transaction checked and rejected.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResultDto.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<ResultDto> Approve(int transactionRecordId, string userName, string roleName, string remarks)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var record = await _context.TransactionRecords
                    .FirstOrDefaultAsync(r => r.Id == transactionRecordId);

                if (record == null) return ResultDto.Failure("Transaction record not found.");
                if (record.status != "PendingApproval") return ResultDto.Failure("Transaction is not in PendingApproval state.");

                var currentStep = await _context.ApprovalSteps
                    .Include(s => s.approvalMatrixSlab)
                    .Where(s => s.approvalMatrixSlabId == record.matrixSlabId && s.stepOrder == record.currentStepOrder)
                    .FirstOrDefaultAsync();

                if (currentStep == null) return ResultDto.Failure("Current approval step not found.");

                var stepRoles = await _context.ApprovalStepRoles
                    .Where(sr => sr.approvalStepId == currentStep.Id)
                    .ToListAsync();

                // Find the specific role from the user's multiple roles that matches this step
                var userRoles = ParseRoles(roleName);
                var matchedStepRole = stepRoles.FirstOrDefault(sr => 
                    userRoles.Any(ur => string.Equals(ur.Trim(), sr.roleName?.Trim(), StringComparison.OrdinalIgnoreCase)));

                if (matchedStepRole == null)
                {
                    var allowedRoles = string.Join(", ", stepRoles.Select(sr => sr.roleName));
                    return ResultDto.Failure($"User role is not authorized for this approval step. Required: [{allowedRoles}]. User roles: '{roleName}'.");
                }

                // The specific role that matched this step
                string effectiveRole = matchedStepRole.roleName;

                // Check rule: Has user or this role already approved this step
                var existingApproval = await _context.TransactionApprovalActions
                    .AnyAsync(a => a.transactionRecordId == record.Id 
                                && a.stepOrder == record.currentStepOrder 
                                && (a.actionByUserName == userName || a.actionByRoleName == effectiveRole || userRoles.Contains(a.actionByRoleName))
                                && a.actionType == "Approved");

                if (existingApproval) return ResultDto.Failure("You have already approved this step.");

                var action = new TransactionApprovalAction
                {
                    transactionRecordId = record.Id,
                    actionType = "Approved",
                    stepOrder = record.currentStepOrder,
                    actionByUserName = userName,
                    actionByRoleName = effectiveRole,
                    actionAt = DateTime.UtcNow,
                    remarks = remarks
                };
                
                _context.TransactionApprovalActions.Add(action);

                bool stepSatisfied = false;
                if (currentStep.approvalLogic == "ANY")
                {
                    stepSatisfied = true;
                }
                else // SINGLE
                {
                    stepSatisfied = true;
                }

                if (stepSatisfied)
                {
                    var nextStep = await _context.ApprovalSteps
                        .Where(s => s.approvalMatrixSlabId == record.matrixSlabId && s.stepOrder == record.currentStepOrder + 1)
                        .FirstOrDefaultAsync();

                    if (nextStep != null)
                    {
                        record.currentStepOrder += 1;
                    }
                    else
                    {
                        record.status = "Approved";
                        
                        // Transactional Outbox Pattern: Insert the event alongside the approval state commit
                        var integrationEvent = new ApprovedTransactionIntegrationEvent
                        {
                            TransactionRecordId = record.Id,
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
                }

                record.updatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResultDto.Success("Transaction approved successfully.");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return ResultDto.Failure("Concurrency conflict: The record was modified by another user. Please try again.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResultDto.Failure($"Error: {ex.Message}");
            }
        }

        public async Task<ResultDto> Reject(int transactionRecordId, string userName, string roleName, string remarks)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var record = await _context.TransactionRecords
                    .FirstOrDefaultAsync(r => r.Id == transactionRecordId);

                if (record == null) return ResultDto.Failure("Transaction record not found.");
                if (record.status != "PendingApproval") return ResultDto.Failure("Transaction is not in PendingApproval state.");

                var currentStep = await _context.ApprovalSteps
                    .Where(s => s.approvalMatrixSlabId == record.matrixSlabId && s.stepOrder == record.currentStepOrder)
                    .FirstOrDefaultAsync();

                if (currentStep == null) return ResultDto.Failure("Current approval step not found.");

                var stepRoles = await _context.ApprovalStepRoles
                    .Where(sr => sr.approvalStepId == currentStep.Id)
                    .ToListAsync();

                // Find the specific role from the user's multiple roles that matches this step
                var userRoles = ParseRoles(roleName);
                var matchedStepRole = stepRoles.FirstOrDefault(sr => 
                    userRoles.Any(ur => string.Equals(ur.Trim(), sr.roleName?.Trim(), StringComparison.OrdinalIgnoreCase)));

                if (matchedStepRole == null)
                {
                    var allowedRoles = string.Join(", ", stepRoles.Select(sr => sr.roleName));
                    return ResultDto.Failure($"User role is not authorized for this approval step. Required: [{allowedRoles}]. User roles: '{roleName}'.");
                }

                string effectiveRole = matchedStepRole.roleName;

                var action = new TransactionApprovalAction
                {
                    transactionRecordId = record.Id,
                    actionType = "Rejected",
                    stepOrder = record.currentStepOrder,
                    actionByUserName = userName,
                    actionByRoleName = effectiveRole,
                    actionAt = DateTime.UtcNow,
                    remarks = remarks
                };
                
                _context.TransactionApprovalActions.Add(action);

                record.status = "Rejected";
                record.updatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResultDto.Success("Transaction rejected successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResultDto.Failure($"Error: {ex.Message}");
            }
        }
    }
}
