using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Configuraci�n de servicios
builder.Services.AddControllers();

// Agrega los servicios de Forwarded Headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Configura el CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder => builder
        .WithOrigins("https://localhost:7278", "https://localhost:6001")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

builder.Services.AddOcelot();

builder.Services.AddIdentityServer()
    .AddInMemoryApiResources(new List<ApiResource>
    {
        new ApiResource("api_resource", "API Resource")
    })
    .AddInMemoryApiScopes(new List<ApiScope>
    {
        new ApiScope("api_scope", "Access to API")
    })
    .AddInMemoryClients(new List<Client>
    {
        new Client
        {
            ClientId = "client",
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
            ClientSecrets = { new Secret("secret".Sha256()) },
            AllowedScopes = { "api_scope" }
        }
    })
    .AddTestUsers(new List<TestUser> // Agregar usuarios de prueba
    {
        new TestUser
        {
            SubjectId = "1",
            Username = "admin",
            Password = "Admin2021*", // Aseg�rate de que coincida con la solicitud
            Claims = new List<Claim>
            {
                new Claim("name", "Administrator"),
                new Claim("role", "Admin")
            }
        }
    })
    .AddDeveloperSigningCredential();


builder.Services.AddAuthorization();

var app = builder.Build();

// Middlewares
app.UseForwardedHeaders();
app.UseIdentityServer();
app.UseCors("CorsPolicy");
app.UseRouting(); // Necesario para habilitar el enrutamiento antes de la autenticaci�n
app.UseAuthentication(); // Middleware de autenticaci�n
app.UseAuthorization(); // Middleware de autorizaci�n
app.MapControllers(); // Aseg�rate de que esto est� presente para que los controladores se registren
await app.UseOcelot(); // Aseg�rate de que esto est� marcado como 'await'
app.Run();