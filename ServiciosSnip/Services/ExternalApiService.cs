using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using ServiciosSnip.Models;

namespace ServiciosSnip.Services
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
            var loginUrl = "https://localhost:7261/api/Auth/login";

            // Body de la solicitud de login
            var loginRequest = new
            {
                username = "test_user",
                password = "password123"
            };

            var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(loginUrl, content);
           
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                // Manejar el error adecuadamente, por ejemplo, registrando el error
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            // Deserializar la respuesta en TokenResponse
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return tokenResponse?.access_token;
        }

        // Obtener datos de fuentes externas con token
        public async Task<FuenteGeneralDto> GetFuentesFinamciamientoAsync(string url, string token)
        {
            var client = _httpClientFactory.CreateClient();

            // Configurar el token en los headers
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // Manejar el error adecuadamente
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FuenteGeneralDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<FuentesExternasDto> GetFuentesExternasAsync(string url, string token)
        {
            var client = _httpClientFactory.CreateClient();

            // Configurar el token en los headers
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // Manejar el error adecuadamente
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FuentesExternasDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<FuenteExternaDto> GetFuenteExternaAsync(string url, string token)
        {
            var client = _httpClientFactory.CreateClient();

            // Configurar el token en los headers
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // Manejar el error adecuadamente
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FuenteExternaDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<FuentesFinanciamientsResponse> GetFuentesFinanciamientosAsync(string url, string token)
        {
            var client = _httpClientFactory.CreateClient();

            // Configurar el token en los headers
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // Manejar el error adecuadamente
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<FuentesFinanciamientsResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        public async Task<CuentaPresupuestariaResponse> GetCuentaPresupuestariaAsync(string url, string token)
        {
            var client = _httpClientFactory.CreateClient();

            // Configurar el token en los headers
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // Manejar el error adecuadamente
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CuentaPresupuestariaResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<CuentasPresupuestariasResponse> GetCuentasPresupuestariasAsync(string url, string token)
        {
            var client = _httpClientFactory.CreateClient();

            // Configurar el token en los headers
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // Manejar el error adecuadamente
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CuentasPresupuestariasResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        

        public async Task<OrganismoFinanciadorResponse> GetOrganismoFinanciadorAsync(string url, string token)
        {
            var client = _httpClientFactory.CreateClient();

            // Configurar el token en los headers
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // Manejar el error adecuadamente
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OrganismoFinanciadorResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<OrganismosFinanciadoresResponsive> GetOrganismosFinanciadoresAsync(string url, string token)
        {
            var client = _httpClientFactory.CreateClient();

            // Configurar el token en los headers
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                // Manejar el error adecuadamente
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OrganismosFinanciadoresResponsive>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        // Clase para deserializar la respuesta del token
        public class TokenResponse
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
        }


    }
}
