using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:7079") // Cambiar al dominio autorizado
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration);

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

var app = builder.Build();

// Configurar Forwarded Headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Middlewares
app.UseCors("CorsPolicy"); // Aplicar la política de CORS
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
await app.UseOcelot();

app.Run();