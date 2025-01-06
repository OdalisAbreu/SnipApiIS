using IdentityServer4.Models;
using IDP.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SnipAuthServerV1.Models;
using System.Data.SqlClient;
using System.Security.Claims;

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

            if (!userProcedureResponse.Contains("activado"))
            {
                return BadRequest("Resultado inesperado del procedimiento almacenado.");
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
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://localhost:7079/connect/token")
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
                RolesArray = await GetUserRoles(userFields[9], connectionString),
                EsUsuarioExterno = userFields[10],
                IdInstitucionUsuario = int.Parse(userFields[11]),
                Cargo = userFields[12],
                EsAdministrador = int.Parse(userFields[13])
            };

            SentrySdk.CaptureMessage($"Usuario {loginRequest.Username} inició sesión a las {DateTime.UtcNow}");
            return Ok(customResponse);
        }

        private async Task<List<RoleInfo>> GetUserRoles(string rolesString, string connectionString)
        {
            var rolesIds = rolesString.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var rolesList = new List<RoleInfo>();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
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

            return rolesList;
        }
    }
}