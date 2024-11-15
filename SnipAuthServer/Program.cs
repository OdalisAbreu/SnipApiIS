using IdentityServer4.Models;
using IdentityServer4.Test;
using IdentityServer4.Validation;
using Microsoft.IdentityModel.Tokens;
using SnipAuthServer.Validators;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Any;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Logging;
using SnipAuthServer.Services;
using SnipAuthServer.Middleware;
using Microsoft.AspNetCore.HttpOverrides;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddTransient<IResourceOwnerPasswordValidator, CustomResourceOwnerPasswordValidator>();
builder.Services.AddSingleton<TokenRevocationService>();

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:7180") // Cambiar al dominio autorizado
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
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
            AllowedScopes = { "api1", "api" },
            AccessTokenLifetime = 1800,
            AllowOfflineAccess = true,
        }
    })//crear otro cliente para revocar el token
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
        options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(5);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://localhost:7180", // Verifica que coincida con el emisor del token
            ValidateAudience = false,
            ValidAudiences = new[] { "api1" }, // Agrega los valores de audiencias permitidas
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

  
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

// Configurar Sentry usando la configuración de appsettings.json
builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.Debug = bool.Parse(builder.Configuration["Sentry:Debug"]);
    options.TracesSampleRate = double.Parse(builder.Configuration["Sentry:TracesSampleRate"]);
    options.AttachStacktrace = bool.Parse(builder.Configuration["Sentry:AttachStacktrace"]);
});

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
        Description = "Ingrese su token JWT a continuación:",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer", 
        BearerFormat = "JWT" 
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
    c.DocumentFilter<ExcludeRoutesDocumentFilter>();// Excluir rutas específicas si es necesario
});


var app = builder.Build();

// Configurar Forwarded Headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});


// Middlewares
app.UseCors("CorsPolicy"); // Aplicar la política de CORS
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthServer API v1");
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    IdentityModelEventSource.ShowPII = true;
}

app.UseSentryTracing();
app.UseRouting();
app.UseIdentityServer(); 
app.UseAuthentication();
app.UseMiddleware<TokenRevocationMiddleware>(); // Middleware de revocación de token
app.UseAuthorization();
app.MapControllers();

app.Run();

//Elimina del Swagger los controladores que no se desea mostrar 
public class ExcludeRoutesDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var excludedRoutes = new[] { "/outputcache/{region}", "/configuration", "/api/secure/data", "/v1/Swagger" };
        foreach (var route in excludedRoutes)
        {
            var key = swaggerDoc.Paths.Keys.FirstOrDefault(k => k.Contains(route));
            if (key != null)
            {
                swaggerDoc.Paths.Remove(key);
            }
        }
    }
}

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

