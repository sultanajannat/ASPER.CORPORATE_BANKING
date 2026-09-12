using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ASPER.CORPORATE_BANKING.Application.DTOs;
using ASPER.CORPORATE_BANKING.Application.Interfaces;

namespace ASPER.CORPORATE_BANKING.Controllers
{
    [ApiController]
    [Route("api/admin-config")]
    public class AdminConfigController : ControllerBase
    {
        private readonly IAdminConfigService _adminConfigService;

        public AdminConfigController(IAdminConfigService adminConfigService)
        {
            _adminConfigService = adminConfigService;
        }

        [HttpPost("transaction-types")]
        public async Task<IActionResult> CreateTransactionType([FromBody] CreateTransactionTypeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return BadRequest("Transaction type code is required.");

            var result = await _adminConfigService.CreateTransactionTypeAsync(request);
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result);
        }

        [HttpPost("transaction-types/{id}/file-template")]
        public async Task<IActionResult> PublishFileTemplate(int id, [FromBody] CreateFileTemplateRequest request)
        {
            if (request.Mappings == null || request.Mappings.Count == 0)
                return BadRequest("At least one mapping is required.");

            var result = await _adminConfigService.PublishFileTemplateAsync(id, request);
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result);
        }

        [HttpPost("transaction-types/{id}/approval-matrix")]
        public async Task<IActionResult> PublishApprovalMatrix(int id, [FromBody] PublishApprovalMatrixRequest request)
        {
            // For now, assume actionByUserId = 1 (System/Admin).
            // In a real scenario, this would come from the JWT claims (e.g., User.FindFirst("UserId").Value).
            int actionByUserId = 1; 

            var result = await _adminConfigService.PublishApprovalMatrixAsync(id, request, actionByUserId);
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result);
        }
    }
}
