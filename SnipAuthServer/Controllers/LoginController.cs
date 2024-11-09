using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using IdentityModel.Client;
using SnipAuthServer.Models;

namespace SnipAuthServer.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public LoginController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // Modelo para recibir el JSON con las credenciales
        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
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
    }
}