using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SigefApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Simulación de validación de credenciales
            if (request.Username == "test_user" && request.Password == "password123")
            {
                // Crear el token JWT
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your_secret_key_here")); // Usa la misma clave que en Program.cs
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, request.Username),
                    new Claim(JwtRegisteredClaimNames.Email, "test_user@example.com"),
                    new Claim("role", "User")
                };

                var token = new JwtSecurityToken(
                    issuer: "your_issuer",
                    audience: "your_audience",
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(30),
                    signingCredentials: creds
                );

                var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new
                {
                    access_token = tokenValue,
                    expires_in = 1800 // 30 minutos
                });
            }

            return Unauthorized(new { error = "Invalid username or password" });
        }
    }

    // Modelo para la solicitud de login
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}