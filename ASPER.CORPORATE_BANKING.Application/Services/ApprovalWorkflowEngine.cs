using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ASPER.CORPORATE_BANKING.Application.Interfaces;
using ASPER.CORPORATE_BANKING.Application.DTOs;
using ASPER.CORPORATE_BANKING.Infrastructure.Data;
using ASPER.CORPORATE_BANKING.Domain.Entities;

namespace ASPER.CORPORATE_BANKING.Application.Services
{
    public class ApprovalWorkflowEngine : IApprovalWorkflowEngine
    {
        private readonly CORPORATE_BANKINGDbContext _context;

        public ApprovalWorkflowEngine(CORPORATE_BANKINGDbContext context)
        {
            _context = context;
        }

        public async Task<ResultDto> CheckAndForward(int transactionRecordId, int userId, int roleId, string remarks)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var record = await _context.TransactionRecords
                    .Include(r => r.matrixSlab)
                    .FirstOrDefaultAsync(r => r.Id == transactionRecordId);

                if (record == null) return ResultDto.Failure("Transaction record not found.");
                if (record.status != "PendingCheck") return ResultDto.Failure("Transaction is not in PendingCheck state.");

                if (record.matrixSlab.checkerRoleId != roleId)
                {
                    return ResultDto.Failure("User role is not authorized to check this transaction.");
                }

                var action = new TransactionApprovalAction
                {
                    transactionRecordId = record.Id,
                    actionType = "Checked",
                    actionByUserId = userId,
                    actionByRoleId = roleId,
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

        public async Task<ResultDto> CheckReject(int transactionRecordId, int userId, int roleId, string remarks)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var record = await _context.TransactionRecords
                    .Include(r => r.matrixSlab)
                    .FirstOrDefaultAsync(r => r.Id == transactionRecordId);

                if (record == null) return ResultDto.Failure("Transaction record not found.");
                if (record.status != "PendingCheck") return ResultDto.Failure("Transaction is not in PendingCheck state.");

                if (record.matrixSlab.checkerRoleId != roleId)
                {
                    return ResultDto.Failure("User role is not authorized to check this transaction.");
                }

                var action = new TransactionApprovalAction
                {
                    transactionRecordId = record.Id,
                    actionType = "Rejected",
                    actionByUserId = userId,
                    actionByRoleId = roleId,
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

        public async Task<ResultDto> Approve(int transactionRecordId, int userId, int roleId, string remarks)
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

                if (!stepRoles.Any(sr => sr.roleId == roleId))
                {
                    return ResultDto.Failure("User role is not authorized for this approval step.");
                }

                var existingApproval = await _context.TransactionApprovalActions
                    .AnyAsync(a => a.transactionRecordId == record.Id 
                                && a.stepOrder == record.currentStepOrder 
                                && a.actionByRoleId == roleId 
                                && a.actionType == "Approved");

                if (existingApproval) return ResultDto.Failure("You have already approved this step.");

                var action = new TransactionApprovalAction
                {
                    transactionRecordId = record.Id,
                    actionType = "Approved",
                    stepOrder = record.currentStepOrder,
                    actionByUserId = userId,
                    actionByRoleId = roleId,
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
                        // Downstream outbox processing should be enqueued here
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

        public async Task<ResultDto> Reject(int transactionRecordId, int userId, int roleId, string remarks)
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

                if (!stepRoles.Any(sr => sr.roleId == roleId))
                {
                    return ResultDto.Failure("User role is not authorized for this approval step.");
                }

                var action = new TransactionApprovalAction
                {
                    transactionRecordId = record.Id,
                    actionType = "Rejected",
                    stepOrder = record.currentStepOrder,
                    actionByUserId = userId,
                    actionByRoleId = roleId,
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
