namespace SnipAuthServerV1.Models
{
    public class CuentaPresupuestariaResponse
    {
        public string cod_tipo { get; set; }
        public string? descripcion_servicios_personales { get; set; }
        public string cod_objeto { get; set; }
        public string descripcion_objeto { get; set; }
        public string cod_cuenta { get; set; }
        public string descripcion_cuenta { get; set; }
        public string cod_sub_cuenta { get; set; }
        public string descripcion_sub_cuenta { get; set; }
        public string cod_auxiliar { get; set; }
        public string descripcion_auxiliar { get; set; }
        public string? estado { get; set; }
        public string? condicion { get; set; }
    }

    public class CuentasPresupuestariasResponse
    {
        public List<CuentaPresupuestariaResponse>? datos { get; set; }
    }

}
