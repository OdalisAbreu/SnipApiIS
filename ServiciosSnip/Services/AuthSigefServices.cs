using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace ServiciosSnip.Services
{
    public class AuthSigefServices
    {
        private readonly IConfiguration _configuration;

        public AuthSigefServices(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Authenticate(string username, string password)
        {
            var configuredUsername = _configuration["AuthenticationSigef:Username"];
            var configuredPassword = _configuration["AuthenticationSigef:Password"];

            if (username == configuredUsername && password == configuredPassword)
            {
                var jwtSettings = _configuration.GetSection("AuthenticationSigef:JwtSettings");
                return GenerateJwtToken(jwtSettings["Issuer"], jwtSettings["Audience"], jwtSettings["SecretKey"], int.Parse(jwtSettings["ExpiresInMinutes"]));
            }

            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        private string GenerateJwtToken(string issuer, string audience, string secretKey, int expiresInMinutes)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                expires: DateTime.Now.AddMinutes(expiresInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
