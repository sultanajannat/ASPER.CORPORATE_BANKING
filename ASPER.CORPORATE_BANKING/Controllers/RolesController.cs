using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ASPER.CORPORATE_BANKING.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace ASPER.CORPORATE_BANKING.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RolesController : ControllerBase
    {
        private readonly IAuthIntegrationService _authIntegrationService;

        public RolesController(IAuthIntegrationService authIntegrationService)
        {
            _authIntegrationService = authIntegrationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var response = await _authIntegrationService.GetAllRolesAsync();
                return Ok(response);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch roles from Auth service", error = ex.Message });
            }
        }
    }
}
