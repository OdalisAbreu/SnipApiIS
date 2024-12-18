namespace CatalogosSnipSigef.Models
{
    public class FuenteFinanciamientoResponse
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
    public class FuentesFinanciamientsResponse
    {
        public List<FuenteFinanciamientoResponse>? datos { get; set; }
    }
}
