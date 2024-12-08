using System.Collections.Generic;

namespace ServiciosSnip.Models
{
    public class FuentesResponse
    {
        public int TotalRegistros { get; set; }
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public List<FuenteExternaDto> Datos { get; set; }
    }
}