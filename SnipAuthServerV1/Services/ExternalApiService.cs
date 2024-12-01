using SnipAuthServerV1.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace SnipAuthServerV1.Services
{
    public class ExternalApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ExternalApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Obtener token de autenticación
        public async Task<string> GetAuthTokenAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var tokenUrl = "https://localhost:7261/connect/token"; // Reemplaza con el puerto correcto de tu IdentityServer

            var clientId = "your_client_id"; // Reemplaza con tu ClientId real
            var clientSecret = "your_client_secret"; // Reemplaza con tu ClientSecret real

            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", "test_user" },
                { "password", "password123" },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "scope", "api_scope" }
            };

            var content = new FormUrlEncodedContent(parameters);

            var response = await client.PostAsync(tokenUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                // Maneja el error adecuadamente
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return tokenResponse?.AccessToken;
        }

        // Obtener datos de fuentes externas con token
        public async Task<FuentesResponse> GetFuentesExternasAsync(string url, string token)
        {
            var client = _httpClientFactory.CreateClient();

            // Configurar el token en los headers
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null; // Error al consumir el servicio externo
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FuentesResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }

    // Clase para deserializar la respuesta del token
    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
    }
}