using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecurityGuardAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/zapier")]
public sealed class ZapierController : ControllerBase
{
    private readonly ILogger<ZapierController> _logger;

    public ZapierController(ILogger<ZapierController> logger)
    {
        _logger = logger;
    }

    [HttpPost("receive")]
    [Consumes("application/json")]
    public IActionResult Receive([FromBody] object payload)
    {
        _logger.LogInformation("Successfully authenticated and received secure payload from Zapier.");
        return Ok(new { message = "Data integrated safely under JWT shield." });
    }
}
