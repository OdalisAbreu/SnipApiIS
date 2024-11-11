using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data.SqlClient;
using IdentityModel.Client;
using SnipAuthServer.Models;

using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using SnipAuthServer.Services;

namespace SnipAuthServer.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly TokenRevocationService _tokenRevocationService;

        public LoginController(IHttpClientFactory httpClientFactory, IConfiguration configuration, TokenRevocationService tokenRevocationService)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _tokenRevocationService = tokenRevocationService;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Username) || string.IsNullOrEmpty(loginRequest.Password))
            {
                return BadRequest("Las credenciales de inicio de sesión son requeridas.");
            }

            // Conectar a la base de datos y ejecutar el procedimiento almacenado
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            string userProcedureResponse = null;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("dbo.f_usuarios_seg_estado", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@pa_cuenta_nom", loginRequest.Username);
                    command.Parameters.AddWithValue("@pa_cuenta_pas", loginRequest.Password);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            userProcedureResponse = reader[0].ToString();
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(userProcedureResponse))
            {
                return BadRequest("Usuario o contraseña incorrectos.");
            }

            // Verifica si el procedimiento almacenado devolvió un resultado esperado
            if (!userProcedureResponse.Contains("activado"))
            {
                return BadRequest("Resultado inesperado del procedimiento almacenado.");
            }

            var client = _httpClientFactory.CreateClient();

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7180/connect/token")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("client_id", "client"),
                    new KeyValuePair<string, string>("client_secret", "secret"),
                    new KeyValuePair<string, string>("username", loginRequest.Username),
                    new KeyValuePair<string, string>("password", loginRequest.Password),
                    new KeyValuePair<string, string>("scope", "api1")
                })
            };

            var response = await client.SendAsync(tokenRequest);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<SnipAuthServer.Models.TokenResponse>(content);

                // Parsear la respuesta del procedimiento almacenado
                var userFields = userProcedureResponse.Split(';');

                // Crear la respuesta personalizada
                var customResponse = new CustomLoginResponse
                {
                    AccessToken = tokenResponse.AccessToken,
                    ExpiresIn = tokenResponse.ExpiresIn,
                    TokenType = tokenResponse.TokenType,
                    Scope = tokenResponse.Scope,
                    IdUsuario = int.Parse(userFields[0]),
                    EstadoUsuario = userFields[1],
                    Nombre = userFields[2],
                    ApellidoPaterno = userFields[3],
                    ApellidoMaterno = userFields[4],
                    Email = userFields[5],
                    UsuarioActivo = userFields[6],
                    Username = userFields[7],
                    Fecha = userFields[8],
                    Roles = userFields[9],
                    EsUsuarioExterno = userFields[10],
                    IdInstitucionUsuario = int.Parse(userFields[11]),
                    Cargo = userFields[12],
                    EsAdministrador = int.Parse(userFields[13])
                };

                return Ok(customResponse);
            }

            return BadRequest("Error al autenticar");
        }

        [HttpPost("revokeToken")]
        //public async Task<IActionResult> RevokeToken()
        public IActionResult RevokeToken()
        {
            /*  // Obtener el token del encabezado Authorization
              if (!Request.Headers.TryGetValue("Authorization", out var tokenHeader))
              {
                  return BadRequest(new { message = "El encabezado Authorization es obligatorio." });
              }

              // Verifica que el token esté en el formato "Bearer {token}"
              var token = tokenHeader.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

              if (string.IsNullOrEmpty(token))
              {
                  return BadRequest(new { message = "El token no se encuentra en el encabezado Authorization." });
              }

              // Define el tipo de token hint si es necesario (por ejemplo: "access_token" o "refresh_token")
               var tokenTypeHint = "access_token"; // Puedes cambiar esto si necesitas revocar un tipo de token diferente

               var client = _httpClientFactory.CreateClient();
               var revocationRequest = new TokenRevocationRequest
               {
                   Address = "https://localhost:7180/connect/revocation",
                   ClientId = "client",
                   ClientSecret = "secret",
                   Token = token,
                   TokenTypeHint = tokenTypeHint 
               };

               var response = await client.RevokeTokenAsync(revocationRequest);
               if (response.IsError)
               {
                   return BadRequest(new { error = "Error al revocar el token." });
               }

               return Ok(new { message = "Token revocado exitosamente." });*/
            // Eliminar el prefijo "Bearer " y agregar el token a la lista negra

            //************************* Método temporal hasta tener acceso a la base de datos *******************************
            if (!Request.Headers.TryGetValue("Authorization", out var tokenHeader))
            {
                return BadRequest(new { message = "El encabezado Authorization es obligatorio." });
            }

            // Verifica que el token esté en el formato "Bearer {token}"
            var token = tokenHeader.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { message = "El token no se encuentra en el encabezado Authorization." });
            }

            // Decodificar el token para obtener el ID del token (Jti)
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwtToken = handler.ReadJwtToken(token);
                var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrEmpty(jti))
                {
                    // Agregar el token a la lista negra
                    _tokenRevocationService.RevokeToken(jti);
                    return Ok(new { message = "Token revocado exitosamente." });
                }
                else
                {
                    return BadRequest(new { message = "No se encontró el ID del token (Jti)." });
                }
            }
            else
            {
                return BadRequest(new { message = "Token no válido." });
            }
            //***************************************************************************************************************

        }
    }
}