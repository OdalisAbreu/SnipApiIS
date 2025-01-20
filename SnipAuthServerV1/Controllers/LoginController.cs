using IdentityServer4.Models;
using IDP.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SnipAuthServerV1.Models;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;
using Sentry;
using System.Data.Common;

namespace SnipAuthServerV1.Controllers
{
    [ApiController]
    [Route("servicios/v1/auth/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IDbConnection _dbConnection;

        public LoginController(IHttpClientFactory httpClientFactory,
                               IConfiguration configuration,
                               IDbConnection dbConnection)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _dbConnection = dbConnection;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (loginRequest == null ||
                string.IsNullOrEmpty(loginRequest.Username) ||
                string.IsNullOrEmpty(loginRequest.Password))
            {
                return BadRequest("Las credenciales de inicio de sesión son requeridas.");
            }

            // Conectar a la base de datos y ejecutar el procedimiento almacenado
            string userProcedureResponse = null;
            var rolesList = new List<RoleInfo>();
            using (_dbConnection)
            {
                if (_dbConnection.State != ConnectionState.Open)
                {
                    _dbConnection.Open(); // Abre la conexión si no está abierta
                }

                using (var command = _dbConnection.CreateCommand())
                {
                    command.CommandText = "dbo.f_usuarios_seg_estado";
                    command.CommandType = CommandType.StoredProcedure;

                    // Parámetros de entrada
                    var usernameParam = command.CreateParameter();
                    usernameParam.ParameterName = "@pa_cuenta_nom";
                    usernameParam.Value = loginRequest.Username;
                    command.Parameters.Add(usernameParam);

                    var passwordParam = command.CreateParameter();
                    passwordParam.ParameterName = "@pa_cuenta_pas";
                    passwordParam.Value = loginRequest.Password;
                    command.Parameters.Add(passwordParam);

                    // Ejecutar el procedimiento y capturar el resultado
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        userProcedureResponse = result.ToString();
                    }
                }

                if (string.IsNullOrEmpty(userProcedureResponse))
                    return BadRequest("Usuario o contraseña incorrectos.");
                {
                }

                if (!userProcedureResponse.Contains("activado"))
                {
                    return BadRequest(userProcedureResponse);
                }

                // Parsear la respuesta del procedimiento almacenado
                var userFields0 = userProcedureResponse.Split(';');

                if (userFields0.Length < 14)
                {
                    return BadRequest("La respuesta del procedimiento almacenado no tiene el formato esperado.");
                }

                var rolesString = userFields0[9];

                var rolesIds = rolesString.Split('.', StringSplitOptions.RemoveEmptyEntries);

                
                var query = $"SELECT rol, rol_des FROM roles WHERE rol IN ({string.Join(",", rolesIds.Select((_, i) => $"@rol{i}"))})";

                using (var command = _dbConnection.CreateCommand())
                {
                    command.CommandText = query;

                    for (int i = 0; i < rolesIds.Length; i++)
                    {
                        var parameter = command.CreateParameter();
                        parameter.ParameterName = $"@rol{i}";
                        parameter.Value = int.Parse(rolesIds[i]); // Asegúrate del tipo de datos.
                        command.Parameters.Add(parameter);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rolesList.Add(new RoleInfo
                            {
                                Rol = reader.GetInt32(0),
                                RolDesc = reader.GetString(1)
                            });
                        }
                    }
                }


            }

            // Parsear la respuesta del procedimiento almacenado
            var userFields = userProcedureResponse.Split(';');

            if (userFields.Length < 14)
            {
                return BadRequest("La respuesta del procedimiento almacenado no tiene el formato esperado.");
            }

            // Preparar la solicitud de token para IdentityServer
            var tokenRequestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", "client_id"),
                new KeyValuePair<string, string>("client_secret", "client_secret"),
                new KeyValuePair<string, string>("username", loginRequest.Username),
                new KeyValuePair<string, string>("password", loginRequest.Password),
                new KeyValuePair<string, string>("scope", "openid profile api_scope offline_access")
            });

            var client = _httpClientFactory.CreateClient();
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://localhost:6002/connect/token")
            {
                Content = tokenRequestContent
            };

            var response = await client.SendAsync(tokenRequest);
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest("Error al autenticar en IdentityServer.");
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<CustomTokenResponse>(content);

              // Crear la respuesta personalizada
            var customResponse = new CustomLoginResponse
            {
                AccessToken = tokenResponse.AccessToken,
                ExpiresIn = tokenResponse.ExpiresIn,
                TokenType = tokenResponse.TokenType,
                RefreshToken = tokenResponse.RefreshToken,
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
                RolesArray = rolesList,
                EsUsuarioExterno = userFields[10],
                IdInstitucionUsuario = int.Parse(userFields[11]),
                Cargo = userFields[12],
                EsAdministrador = int.Parse(userFields[13])
            };

            // Registrar en Sentry (si lo deseas mantener)
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
            var revokeRequestMessage = new HttpRequestMessage(HttpMethod.Post, "https://localhost:6002/connect/revocation")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("token", token),
                    new KeyValuePair<string, string>("token_type_hint", "access_token"),
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