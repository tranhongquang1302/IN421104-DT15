using Microsoft.AspNetCore.Mvc;

namespace LangCenter.API.Controllers;

[ApiController]
[Route("healthz")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth() => Ok(new { status = "Healthy" });
}
