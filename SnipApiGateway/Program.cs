using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net.Http.Headers;
using MMLib.SwaggerForOcelot;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:7079") // Cambia al dominio autorizado
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Configura SwaggerForOcelot
builder.Services.AddSwaggerForOcelot(builder.Configuration);


// Configura autenticación y autorización
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://localhost:7079"; // Dirección de IdentityServer4
        options.RequireHttpsMetadata = true;
        options.Audience = "api_scope"; // El scope definido en tu AuthServer
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var httpClient = context.HttpContext.RequestServices.GetRequiredService<HttpClient>();
                var token = context.SecurityToken as System.IdentityModel.Tokens.Jwt.JwtSecurityToken;

                if (token != null)
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("client_id:client_secret")));
                    var response = await httpClient.PostAsync(
                        "https://localhost:7079/connect/introspect",
                        new FormUrlEncodedContent(new Dictionary<string, string> { { "token", token.RawData } }));

                    if (!response.IsSuccessStatusCode)
                    {
                        context.Fail("Introspection failed");
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization();
// Configura Ocelot (llamar solo una vez y después de SwaggerForOcelot)
builder.Services.AddOcelot(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();

// Configura Swagger para el API Gateway
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

var app = builder.Build();

// Configurar Forwarded Headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseCors("CorsPolicy"); // Aplica la política de CORS
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Configura Swagger y Swagger UI para Ocelot
app.UseSwagger();
app.UseSwaggerForOcelotUI(opt =>
{
    opt.PathToSwaggerGenerator = "/swagger/docs";
});

// Configura Ocelot
await app.UseOcelot();

app.Run();