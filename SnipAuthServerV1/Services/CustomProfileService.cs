using IdentityServer4.Models;
using IdentityServer4.Services;
using System.Security.Claims;

namespace IDP.Services
{
    public class CustomProfileService : IProfileService
    {
        private readonly ILogger<CustomProfileService> _logger;

        public CustomProfileService(ILogger<CustomProfileService> logger)
        {
            _logger = logger;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            // Obtener datos del sujeto (usuario autenticado)
            var userId = context.Subject.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = context.Subject.FindFirst(ClaimTypes.Name)?.Value;

            // Agregar claims al token
            context.IssuedClaims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            context.IssuedClaims.Add(new Claim(ClaimTypes.Name, userName));

            await Task.CompletedTask;
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            // Log para verificar que IsActiveAsync fue llamado
            _logger.LogInformation("Se ejecutó IsActiveAsync.");

            context.IsActive = true;
            await Task.CompletedTask;
        }
    }
}
