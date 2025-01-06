using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using ServiciosSnip.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configurar la conexión a la base de datos
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    return new SqlConnection(connectionString);
});

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