using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ASPER.CORPORATE_BANKING.Application.Interfaces;

namespace ASPER.CORPORATE_BANKING.Controllers
{
    [ApiController]
    [Route("api/admin/monitoring")]
    [Authorize]
    public class AdminMonitoringController : ControllerBase
    {
        private readonly IAdminMonitoringService _monitoringService;

        public AdminMonitoringController(IAdminMonitoringService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        [HttpGet("pending-transactions")]
        public async Task<IActionResult> GetStuckTransactions()
        {
            var result = await _monitoringService.GetPendingTransactionsAsync();
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] string status, 
            [FromQuery] int? typeId, 
            [FromQuery] DateTime? dateFrom, 
            [FromQuery] DateTime? dateTo, 
            [FromQuery] decimal? minAmount, 
            [FromQuery] decimal? maxAmount)
        {
            var result = await _monitoringService.GetTransactionsAsync(status, typeId, dateFrom, dateTo, minAmount, maxAmount);
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("batches")]
        public async Task<IActionResult> GetAllBatches()
        {
            var result = await _monitoringService.GetAllBatchesAsync();
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("batches/{id}/timeline")]
        public async Task<IActionResult> GetBatchTimeline(int id)
        {
            var result = await _monitoringService.GetBatchTimelineAsync(id);
            if (!result.IsSuccess)
                return NotFound(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("transactions/{id}")]
        public async Task<IActionResult> GetTransactionDetail(int id)
        {
            var result = await _monitoringService.GetTransactionDetailAsync(id);
            if (!result.IsSuccess)
                return NotFound(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("transactions/{id}/audit-trail")]
        public async Task<IActionResult> GetAuditTrail(int id)
        {
            var result = await _monitoringService.GetTransactionAuditAsync(id);
            if (!result.IsSuccess)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }
    }
}
