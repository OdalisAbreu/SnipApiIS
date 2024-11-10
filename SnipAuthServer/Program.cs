using IdentityServer4.Models;
using IdentityServer4.Test;
using IdentityServer4.Validation;
using Microsoft.IdentityModel.Tokens;
using SnipAuthServer.Validators;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Any;


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

// Configuración de Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuthServer API",
        Version = "v1",
        Description = "API de autenticación con IdentityServer4 y otros servicios"
    });

    // Configuración de seguridad para el token de autenticación
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingrese 'Bearer' [espacio] y luego el token en el campo de texto.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            new string[] {}
        }
    });
    c.DocumentFilter<ExcludeRoutesDocumentFilter>();
});


var app = builder.Build();

// Middlewares
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthServer API v1");
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}


app.UseRouting();
app.UseIdentityServer(); 
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

//Elimina del Swagger los controladores que no se desea mostrar 
public class ExcludeRoutesDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var excludedRoutes = new[] { "/outputcache/{region}", "/configuration", "/api/secure/data" };

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

