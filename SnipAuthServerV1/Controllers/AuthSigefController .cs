using Microsoft.AspNetCore.Mvc;
using SnipAuthServerV1.Services;

namespace SnipAuthServerV1.Controllers
{
    [ApiController]
    [Route("api/Auth")]
    public class AuthSigefController : Controller
    {
        private readonly AuthSigefServices _authService;

        public AuthSigefController(AuthSigefServices authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginSigefRequest request)
        {
            try
            {
                var token = _authService.Authenticate(request.Username, request.Password);
                return Ok(new
                {
                    access_token = token,
                    expires_in = 1800
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
        }
    }


    public class LoginSigefRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
