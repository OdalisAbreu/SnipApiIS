using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class SecureController : ControllerBase
{
    [HttpGet("data")]
    [Authorize(Policy = "api1")]
    public IActionResult GetData()
    {
        return Ok(new { Message = "Esta es una respuesta segura desde el API con .NET 8." });
    }
}
