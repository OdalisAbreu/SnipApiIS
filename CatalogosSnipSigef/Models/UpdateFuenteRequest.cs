namespace CatalogosSnipSigef.Models
{
    public class UpdateFuenteRequest
    {
        public int? id_version { get; set; }
        public string? cod_fte_gral { get; set; }
        public string? descripcion { get; set; }
        public string? tipo_fuente { get; set; } // "I" o "E"
        public string? activo { get; set; } // "S" o "N"
        public string? estado { get; set; } // "actualizar", "registrado", etc.
        public int? bandeja { get; set; }
        public int? usu_ins { get; set; }
        public DateTime? fec_ins { get; set; }
    }
}
