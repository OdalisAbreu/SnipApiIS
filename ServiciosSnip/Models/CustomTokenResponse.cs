using Newtonsoft.Json;

namespace ServiciosSnip.Models
{
    public class CustomTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        // Campos adicionales del procedimiento almacenado
        [JsonProperty("id_usuario")]
        public int IdUsuario { get; set; }

        [JsonProperty("estado_usuario")]
        public string EstadoUsuario { get; set; }

        [JsonProperty("nombre")]
        public string Nombre { get; set; }

        [JsonProperty("apellido_paterno")]
        public string ApellidoPaterno { get; set; }

        [JsonProperty("apellido_materno")]
        public string ApellidoMaterno { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("usuario_activo")]
        public string UsuarioActivo { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("fecha")]
        public string Fecha { get; set; }

        [JsonProperty("roles")]
        public string Roles { get; set; }

        [JsonProperty("es_usuario_externo")]
        public string EsUsuarioExterno { get; set; }

        [JsonProperty("id_institucion_usuario")]
        public int IdInstitucionUsuario { get; set; }

        [JsonProperty("cargo")]
        public string Cargo { get; set; }

        [JsonProperty("es_administrador")]
        public int EsAdministrador { get; set; }
    }
}
