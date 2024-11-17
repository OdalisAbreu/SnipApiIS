using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SnipAuthServerV1.Validators;
using Swashbuckle.AspNetCore.SwaggerGen;
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

// Configuración de Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuthServer API",
        Version = "v1",
        Description = "API de Servicios"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme // Configuración de seguridad para el token de autenticación
    {
        Description = "Ingrese su token a continuación:",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
       // BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    c.DocumentFilter<CustomOrderDocumentFilter>(); // Añadir el filtro de orden de documento
});

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
builder.Services.AddTransient<IResourceOwnerPasswordValidator, SnipAuthServerV1.Validators.LegacyResourceOwnerPasswordValidator>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthServer API v1");
});

app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public class CustomOrderDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Obtiene todas las rutas y las reordena con base en tu lógica
        var orderedPaths = swaggerDoc.Paths.OrderBy(path =>
        {
            if (path.Key.Contains("/api/v1/Login"))
            {
                return 0; // Asigna el primer lugar al endpoint de Login
            }
            return 1; // Asigna a los demás un valor mayor para que vayan después
        }).ToDictionary(x => x.Key, x => x.Value);
        // Reemplaza las rutas en el documento con las rutas ordenadas
        swaggerDoc.Paths = new OpenApiPaths();
        foreach (var path in orderedPaths)
        {
            swaggerDoc.Paths.Add(path.Key, path.Value);
        }
    }
}