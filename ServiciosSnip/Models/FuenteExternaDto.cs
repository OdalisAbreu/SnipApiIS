namespace ServiciosSnip.Models
{
    public class FuenteExternaDto
    {
        public string cod_grupo { get; set; }
        public string descripcion_grupo { get; set; }
        public string cod_fuente { get; set; }
        public string descripcion_fuente { get; set; }
        public string cod_fuente_especifica { get; set; }
        public string descripcion_fuente_especifica { get; set; }
        public string estado { get; set; }
        public string condicion { get; set; }

    }

    public class FuentesExternasDto
    {
        public List<FuenteExternaDto>? datos { get; set; }
    }
}
