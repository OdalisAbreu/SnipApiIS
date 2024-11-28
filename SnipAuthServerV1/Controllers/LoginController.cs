using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SnipAuthServerV1.Models;
using System.Data.SqlClient;


namespace SnipAuthServerV1.Controllers
{
    [ApiController]
    [Route("servicios/v1/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public LoginController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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
                await connection.OpenAsync();
                using (var command = new SqlCommand("dbo.f_usuarios_seg_estado", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@pa_cuenta_nom", loginRequest.Username);
                    command.Parameters.AddWithValue("@pa_cuenta_pas", loginRequest.Password);

                    // Ejecuta el procedimiento almacenado y obtiene la respuesta
                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        userProcedureResponse = result.ToString();
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

            // Solicita el token de acceso a IdentityServer4
            var client = _httpClientFactory.CreateClient();
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7079/connect/token")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", "client_id"),
                    new KeyValuePair<string, string>("client_secret", "client_secret"),
                    new KeyValuePair<string, string>("username", loginRequest.Username),
                    new KeyValuePair<string, string>("password", loginRequest.Password),
                    new KeyValuePair<string, string>("scope", "api_scope")
                })
            };

            var response = await client.SendAsync(tokenRequest);
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest("Error al autenticar en IdentityServer.");
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<CustomTokenResponse>(content);

            // Parsear la respuesta del procedimiento almacenado
            var userFields = userProcedureResponse.Split(';');
            if (userFields.Length < 14)
            {
                return BadRequest("La respuesta del procedimiento almacenado no tiene el formato esperado.");
            }

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
            SentrySdk.CaptureMessage($"Usuario {loginRequest.Username} inició sesión a las {DateTime.UtcNow}");
            return Ok(customResponse);
        }
        [HttpPost("revoke")]
        public async Task<IActionResult> RevokeTokenFromHeader()
        {
            // Obtener el token del encabezado 'Authorization'
            if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
            {
                return BadRequest("El encabezado 'Authorization' es requerido.");
            }

            // Validar el formato del token
            var token = authorizationHeader.ToString().Replace("Bearer ", "").Trim();
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("El token en el encabezado 'Authorization' no es válido.");
            }

            // Solicitar revocación al IdentityServer
            var client = _httpClientFactory.CreateClient();
            var revokeRequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7079/connect/revocation")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("token", token),
                new KeyValuePair<string, string>("token_type_hint", "access_token"), // Asume que es un access token
                new KeyValuePair<string, string>("client_id", "client_id"),
                new KeyValuePair<string, string>("client_secret", "client_secret")
            })
            };

            var response = await client.SendAsync(revokeRequestMessage);
            if (!response.IsSuccessStatusCode)
            {
                SentrySdk.CaptureMessage("Error al revocar el token.");
                return StatusCode((int)response.StatusCode, "Error al revocar el token.");
            }
            SentrySdk.CaptureMessage($"Token de usuario revocado a las {DateTime.UtcNow}");
            return Ok("Token revocado exitosamente.");
        }
    }

}