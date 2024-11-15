using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace SnipAuthServerV1.Controllers
{
    [Route("api/token")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        [HttpPost("validate")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value });
            return Ok(new
            {
                IsValid = true,
                Claims = claims
            });
        }
    }
}
