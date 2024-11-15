using SnipAuthServer.Services;
using System.IdentityModel.Tokens.Jwt;

namespace SnipAuthServer.Middleware
{
    public class TokenRevocationMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenRevocationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, TokenRevocationService tokenRevocationService)
        {
            // Verificar si el usuario está autenticado y si el token es JWT
            if (context.User.Identity.IsAuthenticated && context.User.HasClaim(c => c.Type == JwtRegisteredClaimNames.Jti))
            {
                // Obtener el ID del token (Jti)
                var tokenId = context.User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrEmpty(tokenId) && tokenRevocationService.IsTokenRevoked(tokenId))
                {
                    // Si el token está revocado, devolver un 401 Unauthorized
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"message\": \"El token ha sido revocado y no es válido.\"}");
                    return;
                }
            }

            // Si el token no está revocado, continuar con la solicitud
            await _next(context);
        }
    }
}
