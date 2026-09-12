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
