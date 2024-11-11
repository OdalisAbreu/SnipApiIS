/*using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SnipAuthServer.Controllers
{
    [ApiController]
    [Route("api/secure")]
    public class SecureController : ControllerBase
    {
        [HttpGet("data")]
        [Authorize(Policy = "api")]
        public IActionResult GetData()
        {
            return Ok(new { Message = "Esta es una respuesta segura desde el API." });
        }
    }
}*/
