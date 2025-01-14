using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using CatalogosSnipSigef.Services;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Configurar la conexión a la base de datos
/*builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new SqlConnection(connectionString);
});*/

// Leer y desencriptar la cadena de conexión desde la variable de entorno
string encryptedConnectionString = Environment.GetEnvironmentVariable("DB_SNIP_BID_InterOper");
// Método para desencriptar la cadena de conexión
static string DecryptConnectionString(string encryptedData)
{
    byte[] encryptedBytes = Convert.FromBase64String(encryptedData);
    byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);
    return System.Text.Encoding.UTF8.GetString(decryptedBytes);
}

if (string.IsNullOrEmpty(encryptedConnectionString))
{
    throw new InvalidOperationException("La cadena de conexión no está configurada correctamente.");
}

string connectionString = DecryptConnectionString(encryptedConnectionString);



// Configurar la conexión a la base de datos
builder.Services.AddScoped<IDbConnection>(sp => new SqlConnection(connectionString));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configurar autenticación JWT
builder.Services.AddAuthentication("Bearer")
    .AddOAuth2Introspection("Bearer", options =>
    {
        options.Authority = "https://localhost:7079";
        options.ClientId = "client_id";
        options.ClientSecret = "client_secret";
    });

// Configuración de Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Gateway", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
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
            new string[] { }
        }
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ILogService, LogService>();
// Agregar autorización
builder.Services.AddAuthorization();
// Agregar controladores
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ExternalApiService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
    {
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = "Unauthorized",
            message = "Error de autenticación. Token nulo o invalido."
        };

        // Serializamos el objeto como JSON antes de escribirlo
        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Componentes API v1");
});
// Configurar middleware
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
