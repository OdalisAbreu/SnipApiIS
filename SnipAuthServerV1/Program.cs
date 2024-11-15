using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

var certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
certStore.Open(OpenFlags.ReadOnly);
var signingCert = certStore.Certificates
    .Find(X509FindType.FindByThumbprint, "b7f43ed2d387ec300b166869ca8d4c907cca4db7", validOnly: false)
    .OfType<X509Certificate2>()
    .FirstOrDefault();
certStore.Close();

if (signingCert == null)
{
    throw new Exception("Certificado no encontrado en el almacén.");
}

// Configuración de IdentityServer4
builder.Services.AddIdentityServer()
       .AddInMemoryApiResources(new List<ApiResource>
    {
        new ApiResource("api_resource", "API Resource")
        {
            ApiSecrets = { new Secret("secret1".Sha256()) },
            Scopes = { "api_scope" }
        },
        new ApiResource("client_id", "Introspection Client")
        {
            ApiSecrets = { new Secret("client_secret".Sha256()) },
            Scopes = { "api_scope" }
        }
    })
    .AddInMemoryApiScopes(new List<ApiScope>
    {
        new ApiScope("api_scope", "Access to API")
    })
    .AddInMemoryClients(new List<Client>
    {
        new Client
        {
            ClientId = "client_id",
            AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,
            ClientSecrets = { new Secret("client_secret".Sha256()) },
            AllowedScopes = { "api_resource", "api_scope" },
            AllowOfflineAccess = true,
            AccessTokenType = AccessTokenType.Reference,
            AccessTokenLifetime = 60 * 10, // 10 minutos
            IdentityTokenLifetime = 60 * 10 // 10 minutos
        }
    })
    .AddDeveloperSigningCredential()
    .AddInMemoryIdentityResources(new List<IdentityResource>
    {
        new IdentityResources.OpenId(),
        new IdentityResources.Profile()
    })
    .AddResourceOwnerValidator<LegacyResourceOwnerPasswordValidator>()
    .AddInMemoryPersistedGrants()
    .AddSigningCredential(signingCert);

builder.Services.AddTransient<IResourceOwnerPasswordValidator, LegacyResourceOwnerPasswordValidator>();

// Configurar el middleware de validación de token
builder.Services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
    .AddIdentityServerAuthentication(options =>
    {
        options.Authority = "https://localhost:7079"; // Cambia al URL de tu IdentityServer
        options.ApiName = "api_resource"; // El nombre del recurso API definido en IdentityServer
        options.ApiSecret = "secret1"; // El secreto configurado para el API
        options.RequireHttpsMetadata = false; // Solo usa 'false' para desarrollo
    });

builder.Services.AddControllers();

var app = builder.Build();

app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();


// Clase personalizada para validar Resource Owner Password
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