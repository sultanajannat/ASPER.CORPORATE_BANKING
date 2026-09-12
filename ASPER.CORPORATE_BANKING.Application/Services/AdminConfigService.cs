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
    }
}
