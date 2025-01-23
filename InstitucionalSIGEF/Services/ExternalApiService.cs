using System;
using System.Net.Http.Headers;
using System.Text;
using InstitucionalSIGEF.Models;
using Newtonsoft.Json;

namespace InstitucionalSIGEF.Services
{
    public class ExternalApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ExternalApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        /// <summary>
        /// Obtiene un token de autenticación para la API externa.
        /// </summary>
        /// <returns>Token de autenticación como cadena.</returns>
        public async Task<string> GetAuthTokenAsync()
        {
            var authUrl = _configuration["ExternalApi:AuthUrl"];
            var clientId = _configuration["ExternalApi:ClientId"];
            var clientSecret = _configuration["ExternalApi:ClientSecret"];

            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                client_id = clientId,
                client_secret = clientSecret
            }), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(authUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<AuthResponse>(await response.Content.ReadAsStringAsync());
                return result?.AccessToken ?? string.Empty;
            }

            return string.Empty;
        }

        /// <summary>
        /// Obtiene la lista de clasificadores institucionales desde la API externa.
        /// </summary>
        /// <param name="url">URL de la API externa.</param>
        /// <param name="token">Token de autenticación.</param>
        /// <returns>Lista de clasificadores institucionales.</returns>
        public async Task<List<ClasificadorInstitucionalRequest>> GetClasificadoresAsync(string url, string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<ClasificadorInstitucionalRequest>>(content);
            }

            return new List<ClasificadorInstitucionalRequest>();
        }

    }

    /// <summary>
    /// Modelo para la respuesta de autenticación.
    /// </summary>
    public class AuthResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
