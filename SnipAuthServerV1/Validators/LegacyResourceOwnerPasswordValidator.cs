using IdentityServer4.Models;
using IdentityServer4.Validation;
using System.Security.Claims;

namespace SnipAuthServerV1.Validators
{
    public class LegacyResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            if (context.UserName == "test_user" && context.Password == "password")
            {
                context.Result = new GrantValidationResult(
                    subject: "user_id",
                    authenticationMethod: "custom",
                    claims: new[] { new Claim("role", "user") }
                );
            }
            else
            {
                context.Result = new GrantValidationResult(
                    TokenRequestErrors.InvalidGrant,
                    "Invalid username or password."
                );
            }
        }
    }
}
