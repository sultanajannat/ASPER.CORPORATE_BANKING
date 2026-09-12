using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ASPER.CORPORATE_BANKING.Application.DTOs;
using ASPER.CORPORATE_BANKING.Application.Interfaces;
using ASPER.CORPORATE_BANKING.Domain.Entities;
using ASPER.CORPORATE_BANKING.Infrastructure.Data;
using System.Collections.Generic;

namespace ASPER.CORPORATE_BANKING.Application.Services
{
    public class AdminConfigService : IAdminConfigService
    {
        private readonly CORPORATE_BANKINGDbContext _context;

        public AdminConfigService(CORPORATE_BANKINGDbContext context)
        {
            _context = context;
        }

        public async Task<ResultDto> CreateTransactionTypeAsync(CreateTransactionTypeRequest request)
        {
            var exists = await _context.TransactionTypes.AnyAsync(t => t.code == request.Code);
            if (exists)
                return ResultDto.Failure("Transaction type code already exists.");

            var entity = new TransactionType
            {
                code = request.Code,
                name = request.Name,
                description = request.Description,
                isActive = true
            };

            _context.TransactionTypes.Add(entity);
            await _context.SaveChangesAsync();

            return ResultDto.Success("Transaction type created successfully.");
        }

        public async Task<ResultDto> PublishFileTemplateAsync(int transactionTypeId, CreateFileTemplateRequest request)
        {
            var txType = await _context.TransactionTypes.FindAsync(transactionTypeId);
            if (txType == null)
                return ResultDto.Failure("Transaction type not found.");

            // Deactivate existing
            var existingActive = await _context.FileTemplateConfigs
                .FirstOrDefaultAsync(c => c.transactionTypeId == transactionTypeId && c.isActive);

            int newVersion = 1;
            if (existingActive != null)
            {
                existingActive.isActive = false;
                existingActive.effectiveTo = DateTime.UtcNow;
                newVersion = (existingActive.version ?? 0) + 1;
            }

            var newConfig = new FileTemplateConfig
            {
                transactionTypeId = transactionTypeId,
                version = newVersion,
                effectiveFrom = DateTime.UtcNow,
                isActive = true
            };

            _context.FileTemplateConfigs.Add(newConfig);
            
            // Need to save first to get the ID, or we can just rely on EF Core relationship fixup
            // We'll let EF Core handle it.
            
            foreach (var mappingReq in request.Mappings)
            {
                var mapping = new FileFieldMapping
                {
                    fileTemplateConfig = newConfig, // EF Core links it
                    columnOrder = mappingReq.ColumnOrder,
                    excelColumnName = mappingReq.ExcelColumnName,
                    fieldKey = mappingReq.FieldKey,
                    dataType = mappingReq.DataType,
                    isMandatory = mappingReq.IsMandatory,
                    validationRegex = mappingReq.ValidationRegex,
                    maxLength = mappingReq.MaxLength
                };
                _context.FileFieldMappings.Add(mapping);
            }

            await _context.SaveChangesAsync();

            return ResultDto.Success("File template published successfully.");
        }

        public async Task<ResultDto> PublishApprovalMatrixAsync(int transactionTypeId, PublishApprovalMatrixRequest request, int actionByUserId)
        {
            var txType = await _context.TransactionTypes.FindAsync(transactionTypeId);
            if (txType == null)
                return ResultDto.Failure("Transaction type not found.");

            // VALIDATIONS
            if (request.Slabs == null || !request.Slabs.Any())
                return ResultDto.Failure("Matrix must contain at least one slab.");

            var orderedSlabs = request.Slabs.OrderBy(s => s.SlabOrder).ToList();
            
            for (int i = 0; i < orderedSlabs.Count; i++)
            {
                var current = orderedSlabs[i];

                // Null MaxAmount is only allowed on the last slab
                if (current.MaxAmount == null && i != orderedSlabs.Count - 1)
                {
                    return ResultDto.Failure($"Slab '{current.SlabOrder}' has no MaxAmount, but it is not the last slab. Only the top slab can be open-ended.");
                }

                // Check overlap with previous slab
                if (i > 0)
                {
                    var previous = orderedSlabs[i - 1];
                    if (previous.MaxAmount.HasValue && current.MinAmount <= previous.MaxAmount.Value)
                    {
                        return ResultDto.Failure($"Overlap detected: Slab {current.SlabOrder} MinAmount ({current.MinAmount}) overlaps with Slab {previous.SlabOrder} MaxAmount ({previous.MaxAmount}).");
                    }
                }

                // Validate Steps
                if (current.Steps != null)
                {
                    foreach (var step in current.Steps)
                    {
                        if (step.ApprovalLogic?.ToUpper() == "SINGLE")
                        {
                            if (step.StepRoles == null || step.StepRoles.Count != 1)
                                return ResultDto.Failure($"Step {step.StepOrder} in Slab {current.SlabOrder} has SINGLE logic but does not have exactly one role.");
                        }
                        else if (step.ApprovalLogic?.ToUpper() == "ANY")
                        {
                            if (step.StepRoles == null || !step.StepRoles.Any())
                                return ResultDto.Failure($"Step {step.StepOrder} in Slab {current.SlabOrder} has ANY logic but no roles are assigned.");
                        }
                        else
                        {
                            return ResultDto.Failure($"Invalid ApprovalLogic '{step.ApprovalLogic}' on Step {step.StepOrder} in Slab {current.SlabOrder}. Must be SINGLE or ANY.");
                        }
                    }
                }
            }

            // Deactivate existing
            var existingActive = await _context.ApprovalMatrixConfigs
                .FirstOrDefaultAsync(c => c.transactionTypeId == transactionTypeId && c.isActive);

            int newVersion = 1;
            if (existingActive != null)
            {
                existingActive.isActive = false;
                existingActive.effectiveTo = DateTime.UtcNow;
                newVersion = (existingActive.version ?? 0) + 1;
            }

            var newConfig = new ApprovalMatrixConfig
            {
                transactionTypeId = transactionTypeId,
                version = newVersion,
                effectiveFrom = DateTime.UtcNow,
                isActive = true,
                isApproved = 1, // Assuming auto-approved for now, or 0 if Maker-Checker on config
                approvedByUserId = actionByUserId,
                approvedDate = DateTime.UtcNow
            };

            _context.ApprovalMatrixConfigs.Add(newConfig);

            foreach (var slabReq in orderedSlabs)
            {
                var slab = new ApprovalMatrixSlab
                {
                    approvalMatrixConfig = newConfig,
                    slabOrder = slabReq.SlabOrder,
                    minAmount = slabReq.MinAmount,
                    maxAmount = slabReq.MaxAmount,
                    isAutoApprove = slabReq.IsAutoApprove,
                    isCheckerRequired = slabReq.IsCheckerRequired,
                    checkerRoleId = slabReq.CheckerRoleId,
                    checkerRoleName = slabReq.CheckerRoleName,
                    isActive = true
                };
                _context.ApprovalMatrixSlabs.Add(slab);

                if (slabReq.Steps != null)
                {
                    foreach (var stepReq in slabReq.Steps)
                    {
                        var step = new ApprovalStep
                        {
                            approvalMatrixSlab = slab,
                            stepOrder = stepReq.StepOrder,
                            approvalLogic = stepReq.ApprovalLogic?.ToUpper()
                        };
                        _context.ApprovalSteps.Add(step);

                        if (stepReq.StepRoles != null)
                        {
                            foreach (var roleReq in stepReq.StepRoles)
                            {
                                var stepRole = new ApprovalStepRole
                                {
                                    approvalStep = step,
                                    roleId = roleReq.RoleId,
                                    roleName = roleReq.RoleName
                                };
                                _context.ApprovalStepRoles.Add(stepRole);
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return ResultDto.Success("Approval Matrix published successfully.");
        }

        public async Task<ResultDto> GetTransactionTypesAsync()
        {
            var types = await _context.TransactionTypes.ToListAsync();
            return ResultDto.Success("Fetched transaction types.") is ResultDto res ? new ResultDto { IsSuccess = res.IsSuccess, Message = res.Message, Data = types } : ResultDto.Success();
        }

        public async Task<ResultDto> GetActiveFileTemplateAsync(int transactionTypeId)
        {
            var template = await _context.FileTemplateConfigs
                .Where(t => t.transactionTypeId == transactionTypeId && t.isActive == true)
                .FirstOrDefaultAsync();

            if (template == null) return ResultDto.Failure("No active template found.");
            
            var mappings = await _context.FileFieldMappings
                .Where(m => m.fileTemplateConfigId == template.Id)
                .OrderBy(m => m.columnOrder)
                .ToListAsync();

            var data = new { template, mappings };
            return ResultDto.Success("Fetched active file template.") is ResultDto res ? new ResultDto { IsSuccess = res.IsSuccess, Message = res.Message, Data = data } : ResultDto.Success();
        }

        public async Task<ResultDto> GetActiveApprovalMatrixAsync(int transactionTypeId)
        {
            var matrix = await _context.ApprovalMatrixConfigs
                .Where(m => m.transactionTypeId == transactionTypeId && m.isActive == true)
                .FirstOrDefaultAsync();

            if (matrix == null) return ResultDto.Failure("No active matrix found.");
            
            var slabs = await _context.ApprovalMatrixSlabs
                .Where(s => s.approvalMatrixConfigId == matrix.Id)
                .ToListAsync();
                
            var slabIds = slabs.Select(s => s.Id).ToList();
            var steps = await _context.ApprovalSteps
                .Where(s => slabIds.Contains(s.approvalMatrixSlabId ?? 0))
                .OrderBy(s => s.stepOrder)
                .ToListAsync();
                
            var stepIds = steps.Select(s => s.Id).ToList();
            var stepRoles = await _context.ApprovalStepRoles
                .Where(sr => stepIds.Contains(sr.approvalStepId ?? 0))
                .ToListAsync();

            var data = new { matrix, slabs, steps, stepRoles };
            return ResultDto.Success("Fetched active matrix.") is ResultDto res ? new ResultDto { IsSuccess = res.IsSuccess, Message = res.Message, Data = data } : ResultDto.Success();
        }

        public async Task<ResultDto> GetApprovalMatrixHistoryAsync(int transactionTypeId)
        {
            var history = await _context.ApprovalMatrixConfigs
                .Where(m => m.transactionTypeId == transactionTypeId)
                .OrderByDescending(m => m.version)
                .Select(m => new { m.Id, m.version, m.isActive, m.createdAt, m.createdBy })
                .ToListAsync();

            return ResultDto.Success("Fetched matrix history.") is ResultDto res ? new ResultDto { IsSuccess = res.IsSuccess, Message = res.Message, Data = history } : ResultDto.Success();
        }

        public async Task<ResultDto> ToggleTransactionTypeStatusAsync(int id)
        {
            var txType = await _context.TransactionTypes.FindAsync(id);
            if (txType == null) return ResultDto.Failure("Transaction type not found.");

            txType.isActive = !(txType.isActive ?? false);
            txType.updatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ResultDto.Success($"Transaction type status changed to {(txType.isActive == true ? "Active" : "Inactive")}.");
        }
    }
}
