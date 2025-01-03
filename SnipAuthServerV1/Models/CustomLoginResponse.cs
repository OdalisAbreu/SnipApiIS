using Newtonsoft.Json;
using System.IO;

namespace SnipAuthServerV1.Models
{
    public class CustomLoginResponse
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; }
        public string Scope { get; set; }
        public string RefreshToken { get; set; }
        

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
        //public string Roles { get; set; } // Eliminar esta línea
        public List<RoleInfo> RolesArray { get; set; } // Nueva lista con objetos rol
        public string EsUsuarioExterno { get; set; }
        public int IdInstitucionUsuario { get; set; }
        public string Cargo { get; set; }
        public int EsAdministrador { get; set; }
    }

    public class RoleInfo
    {
        [JsonProperty("rol")]
        public int Rol { get; set; }
        [JsonProperty("rol_desc")]
        public string RolDesc { get; set; }
    }
}
