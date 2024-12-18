using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SnipAuthServerV1.Models;
using System.Data.SqlClient;
using System.IO;


namespace SnipAuthServerV1.Controllers
{
    [ApiController]
    [Route("servicios/v1/auth/[controller]")]
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
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("client_id", "client_id"),
            new KeyValuePair<string, string>("client_secret", "client_secret"),
            new KeyValuePair<string, string>("username", loginRequest.Username),
            new KeyValuePair<string, string>("password", loginRequest.Password),
            new KeyValuePair<string, string>("scope", "api_scope offline_access")
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

            // Obtener roles como string
            var rolesString = userFields[9]; // Ej: "110.130.900.907"

            // Dividir por '.' para obtener IDs individuales
            var rolesIds = rolesString.Split('.', StringSplitOptions.RemoveEmptyEntries);

            // Ahora consultamos la tabla cla_roles para obtener el rol_desc de cada ID
            var rolesList = new List<RoleInfo>(); // donde RoleInfo es una clase que definiremos para mapear {rol, rol_desc}

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                // Usamos un IN dinámico para obtener todos los roles en una sola consulta
                var parameterNames = new List<string>();
                var parameters = new List<SqlParameter>();
                for (int i = 0; i < rolesIds.Length; i++)
                {
                    var paramName = "@rol" + i;
                    parameterNames.Add(paramName);
                    parameters.Add(new SqlParameter(paramName, rolesIds[i]));
                }

                var query = $"SELECT rol, rol_des FROM roles WHERE rol IN ({string.Join(",", parameterNames)})";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
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
                // Ya no asignamos un string a Roles, ahora asignamos la lista de objetos
                RolesArray = rolesList,
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