using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Microsoft.OpenApi.Models;
using SnipAuthServerV1.Validators;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Security.Cryptography.X509Certificates;
using SnipAuthServerV1.Jobs;
using System.Data;
using Microsoft.Data.SqlClient;
using IDP.Services;
using System.Security.Cryptography;

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
    throw new Exception("Certificado no encontrado en el almac�n.");
}

// M�todo para desencriptar la cadena de conexi�n
static string DecryptConnectionString(string encryptedData)
{
    byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
    byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);
    return System.Text.Encoding.UTF8.GetString(decryptedBytes);
}

// Leer la cadena de conexi�n cifrada desde la variable de entorno
string encryptedConnectionString = Environment.GetEnvironmentVariable("DB_SNIP_BID_InterOper");
//Console.WriteLine($"Le�do: {encryptedConnectionString}");

if (string.IsNullOrEmpty(encryptedConnectionString))
{
    throw new InvalidOperationException("La cadena de conexi�n no est� configurada correctamente.");
}

string connectionString = DecryptConnectionString(encryptedConnectionString);

// Configurar la conexi�n a la base de datos
builder.Services.AddScoped<IDbConnection>(sp => new SqlConnection(connectionString));


// Configuraci�n de CORS
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

// Configuraci�n de IdentityServer4
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
            AllowedScopes = { "api_resource", "api_scope", "offline_access", "openid", "profile" },
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
        new IdentityResources.Profile(),
        new IdentityResource
        {
            Name = "offline_access",
            DisplayName = "Offline Access",
            Description = "Access to use refresh tokens",
            UserClaims = new List<string>() // No necesita claims
        }
    })
    .AddResourceOwnerValidator<LegacyResourceOwnerPasswordValidator>()
    .AddInMemoryPersistedGrants()
    .AddSigningCredential(signingCert)
    .AddProfileService<CustomProfileService>();

// Configuraci�n de Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuthServer API",
        Version = "v1",
        Description = "API de Servicios"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme // Configuraci�n de seguridad para el token de autenticaci�n
    {
        Description = "Ingrese su token a continuaci�n:",
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
    c.DocumentFilter<CustomOrderDocumentFilter>(); // A�adir el filtro de orden de documento
});

// Configurar el middleware de validaci�n de token
builder.Services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
    .AddIdentityServerAuthentication(options =>
    {
        options.Authority = builder.Configuration["Access:UrlBase"]; // Cambia al URL de tu IdentityServer
        options.ApiName = "api_resource"; // El nombre del recurso API definido en IdentityServer
        options.ApiSecret = builder.Configuration["Access:ApiSecret"]; // El secreto configurado para el API
        options.RequireHttpsMetadata = true; // Solo usa 'false' para desarrollo
    });

// Configurar Sentry usando la configuraci�n de appsettings.json
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


var app = builder.Build();


app.UseCors("CorsPolicy"); // Aplicar la pol�tica de CORS
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
            return 1; // Asigna a los dem�s un valor mayor para que vayan despu�s
        }).ToDictionary(x => x.Key, x => x.Value);
        swaggerDoc.Paths = new OpenApiPaths();
        foreach (var path in orderedPaths)
        {
            swaggerDoc.Paths.Add(path.Key, path.Value);
        }
    }
}