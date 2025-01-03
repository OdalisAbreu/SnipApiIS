namespace CatalogosSnipSigef.Models
{
    public class UpdateobjetalesRequest
    {
        public string? id_version { get; set; }
        public string? cod_objetal { get; set; }
        public string? descripcion { get; set; }
        public string? cod_objetal_superior { get; set; }
        public string? cod_despliegue { get; set; }
        public string? terminal { get; set; }
        public string? inversion { get; set; }
        public string? activo { get; set; }
        public int? bandeja { get; set; }
        public int? usu_ins { get; set; }
        public DateTime? fec_ins { get; set; }
        public string? usu_upd { get; set; }
        public string? fec_upd { get; set; }
    }
}