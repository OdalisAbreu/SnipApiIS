using Newtonsoft.Json;

namespace CatalogosSnipSigef.Models
{
    public class ClasificadorInstitucionalRequest
    {
        [JsonProperty("id_institucion")]
        public string id_institucion { get; set; }

        [JsonProperty("desc_institucion")]
        public string desc_institucion { get; set; }

        [JsonProperty("sigla")]
        public string sigla { get; set; }

        [JsonProperty("cod_sector")]
        public string cod_sector { get; set; }

        [JsonProperty("cod_subsector")]
        public string cod_subsector { get; set; }

        [JsonProperty("cod_area")]
        public string cod_area { get; set; }

        [JsonProperty("cod_subarea")]
        public string cod_subarea { get; set; }

        [JsonProperty("cod_seccion")]
        public string cod_seccion { get; set; }

        [JsonProperty("cod_poderes_oe")]
        public string cod_poderes_oe { get; set; }

        [JsonProperty("cod_entidades")]
        public string cod_entidades { get; set; }

        [JsonProperty("cod_capitulo")]
        public string cod_capitulo { get; set; }

        [JsonProperty("cod_subcapitulo")]
        public string cod_subcapitulo { get; set; }

        [JsonProperty("cod_ue")]
        public string cod_ue { get; set; }
    }

    public class ClasificadoresInstitucionalesRequest
    {
        public List<ClasificadorInstitucionalRequest>? datos { get; set; }
    }
}
