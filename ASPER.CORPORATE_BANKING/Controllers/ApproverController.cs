using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ASPER.CORPORATE_BANKING.Application.DTOs;
using ASPER.CORPORATE_BANKING.Application.Interfaces;
using ASPER.CORPORATE_BANKING.Infrastructure.Data;

namespace ASPER.CORPORATE_BANKING.Controllers
{
    [ApiController]
    [Route("api/approver")]
    [Authorize]
    public class ApproverController : ControllerBase
    {
        private readonly IApprovalWorkflowEngine _workflowEngine;
        private readonly IAuthIntegrationService _authService;
        private readonly CORPORATE_BANKINGDbContext _context;

        public ApproverController(
            IApprovalWorkflowEngine workflowEngine,
            IAuthIntegrationService authService,
            CORPORATE_BANKINGDbContext context)
        {
            _workflowEngine = workflowEngine;
            _authService = authService;
            _context = context;
        }

        [HttpGet("transactions/pending")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var records = await _context.TransactionRecords
                .Where(r => r.status == "PendingApproval")
                .Select(r => new {
                    r.Id,
                    r.instructionRefNo,
                    r.amount,
                    r.currency,
                    r.status,
                    r.matrixSlabId,
                    r.currentStepOrder
                })
                .ToListAsync();

            return Ok(records);
        }

        [HttpGet("transactions/{id}")]
        public async Task<IActionResult> GetTransactionDetail(int id)
        {
            var record = await _context.TransactionRecords
                .Include(r => r.transactionBatch)
                .Where(r => r.Id == id)
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

            if (record == null) return NotFound("Transaction not found.");
            
            return Ok(record);
        }

        [HttpPost("transactions/{id}/action")]
        public async Task<IActionResult> ProcessApproval(int id, [FromBody] WorkflowActionRequest request)
        {
            try
            {
                // Fallback to "testuser" for dev if claims are empty
                string userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "testuser";
                // In production, we parse UserId from JWT. Fallback to 1.
                int userId = int.TryParse(User.FindFirst("UserId")?.Value, out int uid) ? uid : 1;

                // Validate Role via gRPC Auth Service
                var roleInfo = await _authService.GetRoleInfoByUserAsync(userName);
                if (roleInfo == null || roleInfo.Roles == null || !roleInfo.Roles.Any())
                {
                    return Forbid("User does not possess any valid roles.");
                }

                ResultDto result;
                if (request.IsApproved)
                {
                    result = await _workflowEngine.Approve(id, userId, request.RoleId, request.Remarks);
                }
                else
                {
                    result = await _workflowEngine.Reject(id, userId, request.RoleId, request.Remarks);
                }

                if (!result.IsSuccess)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict("Concurrency conflict: This transaction was already modified by another user.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

