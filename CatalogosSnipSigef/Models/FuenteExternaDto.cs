﻿namespace CatalogosSnipSigef.Models
{
    public class FuenteExterna
    {
        public string cod_finalidad { get; set; }
        public string descripcion_finalidad { get; set; }
        public string cod_funcion { get; set; }
        public string descripcion_funcion { get; set; }
        public string cod_sub_funcion { get; set; }
        public string descripcion_sub_funcion { get; set; }
        public string estado { get; set; }
        public string condicion { get; set; }

    }

    public class FuentesExternasDto
    {
        public List<FuenteExterna>? datos { get; set; }
    }
}
