using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using IdentityServer4.Stores;
using Microsoft.OpenApi.Models;
using SnipAuthServerV1.Validators;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Security.Cryptography.X509Certificates;
using SnipAuthServerV1.Jobs;
using SnipAuthServerV1.Services;
using System.Data;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);


//Buscar el certificado generado
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

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(builder.Configuration["Access:UrlBase"]) 
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new SqlConnection(connectionString);
});

// Configuración de IdentityServer4
builder.Services.AddIdentityServer()
       .AddInMemoryApiResources(new List<ApiResource>
    {
        new ApiResource("api_resource", "API Resource")
        {
            ApiSecrets = { new Secret(builder.Configuration["Access:ApiSecret"].Sha256()) },
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
            ClientId = builder.Configuration["Access:ClientId"],
            AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,
            ClientSecrets = { new Secret(builder.Configuration["Access:ClientSecrets"].Sha256()) },
            AllowedScopes = { "api_resource", "api_scope" },
            AllowOfflineAccess = true,
            AccessTokenType = AccessTokenType.Reference,
            AccessTokenLifetime = 60 * 30,
            IdentityTokenLifetime = 60 * 30 
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
        options.Authority = builder.Configuration["Access:UrlBase"]; // Cambia al URL de tu IdentityServer
        options.ApiName = "api_resource"; // El nombre del recurso API definido en IdentityServer
        options.ApiSecret = builder.Configuration["Access:ApiSecret"]; // El secreto configurado para el API
        options.RequireHttpsMetadata = false; // Solo usa 'false' para desarrollo
    });

// Configurar Sentry usando la configuración de appsettings.json
builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.Debug = bool.Parse(builder.Configuration["Sentry:Debug"]);
    options.TracesSampleRate = double.Parse(builder.Configuration["Sentry:TracesSampleRate"]);
    options.AttachStacktrace = bool.Parse(builder.Configuration["Sentry:AttachStacktrace"]);
});

builder.Services.AddControllers();
builder.Services.AddTransient<IResourceOwnerPasswordValidator, SnipAuthServerV1.Validators.LegacyResourceOwnerPasswordValidator>();
builder.Services.AddSingleton<IHostedService, TokenCleanupJob>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ExternalApiService>();


var app = builder.Build();


app.UseCors("CorsPolicy"); // Aplicar la política de CORS
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
        var orderedPaths = swaggerDoc.Paths.OrderBy(path =>
        {
            if (path.Key.Contains("/servicios/v1/Login"))
            {
                return 0; // Asigna el primer lugar al endpoint de Login
            }
            return 1; // Asigna a los demás un valor mayor para que vayan después
        }).ToDictionary(x => x.Key, x => x.Value);
        swaggerDoc.Paths = new OpenApiPaths();
        foreach (var path in orderedPaths)
        {
            swaggerDoc.Paths.Add(path.Key, path.Value);
        }
    }
}