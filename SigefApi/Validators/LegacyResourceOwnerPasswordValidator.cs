using IdentityServer4.Models;
using IdentityServer4.Validation;
using System.Security.Claims;

namespace SnipAuthServerV1.Validators
{
    public class LegacyResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            // Simulación de validación de credenciales
            if (context.UserName == "test_user" && context.Password == "password123")
            {
                var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim("name", "Test User"),
                new System.Security.Claims.Claim("email", "test_user@example.com")
            };

                // Validar que las afirmaciones no tengan valores nulos
                foreach (var claim in claims)
                {
                    if (string.IsNullOrEmpty(claim.Value))
                    {
                        throw new ArgumentException($"Claim '{claim.Type}' tiene un valor nulo o vacío.");
                    }
                }

                context.Result = new GrantValidationResult(
                    subject: "1",
                    authenticationMethod: "password",
                    claims: claims
                );
            }
            else
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid username or password");
            }

            await Task.CompletedTask;
        }
    }
}
