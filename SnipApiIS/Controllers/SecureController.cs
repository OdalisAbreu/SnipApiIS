using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/secure")]
public class SecureController : ControllerBase
{
    [HttpGet("data")]
    public IActionResult GetData()
    {
        return Ok(new { Message = "Esta es una respuesta segura desde el API." });
    }
}
