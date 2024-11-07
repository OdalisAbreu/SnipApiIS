using IdentityServer4.Models;
using IdentityServer4.Test;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios a la colección de servicios
builder.Services.AddControllers();

// Configuración de IdentityServer4
builder.Services.AddIdentityServer(options =>
{
    options.EmitStaticAudienceClaim = true;
})
.AddInMemoryApiScopes(new List<ApiScope>
{
    new ApiScope("api1", "My API")
})
.AddInMemoryClients(new List<Client>
{
    new Client
    {
        ClientId = "client",
        AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
        ClientSecrets = { new Secret("secret".Sha256()) },
        AllowedScopes = { "api1" }
    }
})
.AddDeveloperSigningCredential() // Agregar una clave de firma en memoria para desarrollo
.AddTestUsers(new List<TestUser>
{
    new TestUser
    {
        SubjectId = "1",
        Username = "admin",
        Password = "Admin2021*"
    }
});

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://localhost:7278"; // Cambia por la URL que uses
        options.RequireHttpsMetadata = false; // Solo para desarrollo
        options.Audience = "https://localhost:7278/resources";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("api1", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "api1");
    });
});

var app = builder.Build();

// Middleware de la aplicación
app.UseRouting();
app.UseIdentityServer(); // Agregar middleware de IdentityServer4
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();