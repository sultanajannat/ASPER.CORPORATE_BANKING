using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ASPER.CORPORATE_BANKING.Application.Interfaces;
using ASPER.CORPORATE_BANKING.Application.DTOs;

namespace ASPER.CORPORATE_BANKING.Controllers
{
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

            // For now, assume makerUserId = 2 (Maker user).
            // In a real scenario, this would come from the JWT claims.
            int makerUserId = 2;
            string makerUserName = "Maker User";

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
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized("Invalid user token.");

            var result = await _fileIngestionService.GetMakerBatchesAsync(userId);
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
