namespace SnipAuthServerV1.Models
{
    public class CustomLoginResponse
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; }
        public string Scope { get; set; }

        // Campos adicionales desde el procedimiento almacenado
        public int IdUsuario { get; set; }
        public string EstadoUsuario { get; set; }
        public string Nombre { get; set; }
        public string ApellidoPaterno { get; set; }
        public string ApellidoMaterno { get; set; }
        public string Email { get; set; }
        public string UsuarioActivo { get; set; }
        public string Username { get; set; }
        public string Fecha { get; set; }
        public string Roles { get; set; }
        public string EsUsuarioExterno { get; set; }
        public int IdInstitucionUsuario { get; set; }
        public string Cargo { get; set; }
        public int EsAdministrador { get; set; }
    }
}
