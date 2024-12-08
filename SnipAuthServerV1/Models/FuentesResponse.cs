using System.Collections.Generic;

namespace SnipAuthServerV1.Models
{
    public class FuentesResponse
    {
        public int TotalRegistros { get; set; }
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public List<FuenteExternaDto> Datos { get; set; }
    }
}