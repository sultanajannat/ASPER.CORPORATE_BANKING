using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ASPER.CORPORATE_BANKING.Application.Interfaces;
using ASPER.CORPORATE_BANKING.Application.DTOs;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ASPER.CORPORATE_BANKING.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/maker")]
    public class MakerController : ControllerBase
    {
        private readonly IFileIngestionService _fileIngestionService;

        public MakerController(IFileIngestionService fileIngestionService)
        {
            _fileIngestionService = fileIngestionService;
        }

        [HttpPost("transactions/batches/upload")]
        public async Task<IActionResult> UploadBatch([FromForm] int transactionTypeId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty or not provided.");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only .xlsx files are supported.");

            // In a real scenario, this would come from the JWT claims.
            string makerUserName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Maker User";
            int makerUserId = 0; // Not used anymore for validation

            using (var stream = file.OpenReadStream())
            {
                var result = await _fileIngestionService.IngestFileAsync(transactionTypeId, stream, file.FileName, makerUserId, makerUserName);

                if (!result.IsSuccess)
                    return BadRequest(result.Message);

                return Ok(result.Data);
            }
        }

        [HttpGet("transactions/batches")]
        public async Task<IActionResult> GetMyBatches()
        {
            // Fallback to "Maker User" if Name claim is not found
            string userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Maker User";

            var result = await _fileIngestionService.GetMakerBatchesAsync(userName);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Message);
        }

        [HttpGet("transactions/batches/{id}/validation-report")]
        public async Task<IActionResult> GetBatchValidationReport(int id)
        {
            var result = await _fileIngestionService.GetBatchValidationReportAsync(id);
            return result.IsSuccess ? Ok(result.Data) : NotFound(result.Message);
        }
    }
}
