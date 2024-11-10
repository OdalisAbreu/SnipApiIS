using IdentityServer4.Models;
using IdentityServer4.Test;
using IdentityServer4.Validation;
using Microsoft.IdentityModel.Tokens;
using SnipAuthServer.Validators;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddTransient<IResourceOwnerPasswordValidator, CustomResourceOwnerPasswordValidator>();

// Configuración de IdentityServer4
builder.Services.AddIdentityServer()
    .AddInMemoryApiScopes(new List<ApiScope>
    {
      //  new ApiScope("api1", new[] { "custom_claim", "Id_usuario", "estado_usuario", "email", "roles" }) // Agrega los claims necesarios
        new ApiScope("api", "Access to API"),
       new ApiScope("api1", "Access to API 1")
    })
    .AddInMemoryClients(new List<Client>
    {
        new Client
        {
            ClientId = "client",
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedScopes = { "api1", "api" }
        }
    })
    .AddInMemoryApiResources(new List<ApiResource>
    {
        new ApiResource("api1", "API 1")
        {
            Scopes = { "api1" }
        },
        new ApiResource("api2", "API 2")
        {
            Scopes = { "api2" }
        }
    })
    .AddDeveloperSigningCredential();


builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://localhost:7180"; // Cambia por la URL que uses
        options.RequireHttpsMetadata = false; // Solo para desarrollo
        options.Audience = "api1";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://localhost:7180", // Verifica que coincida con el emisor del token
            ValidateAudience = false,
            ValidAudiences = new[] { "api1" } // Agrega los valores de audiencias permitidas
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("api", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "api");
    });
    options.AddPolicy("api1", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "api1");
    });
});

var app = builder.Build();

// Middlewares
app.UseRouting();
app.UseIdentityServer(); // Asegúrate de agregar este middleware para habilitar los endpoints de IdentityServer
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();