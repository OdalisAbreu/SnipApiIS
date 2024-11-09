using Microsoft.AspNetCore.Mvc;

namespace SnipApiIS.Controllers
{
    [ApiController]
    [Route("Account")]
    public class AccountController : ControllerBase
    {
        [HttpGet("Login")]
        public IActionResult Login()
        {
            return NotFound(new { Message = "La ruta /Account/Login no está implementada." });
        }
    }
}
