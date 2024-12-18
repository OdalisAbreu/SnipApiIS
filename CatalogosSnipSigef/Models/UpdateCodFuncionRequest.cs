namespace CatalogosSnipSigef.Models
{
    public class UpdateCodFuncionRequest
    {
        public int? id_funcional { get; set; }
        public string? cod_finalidad { get; set; }
        public string? cod_funcion { get; set; }
        public string? cod_sub_funcion { get; set; }
        public string? descripcion { get; set; }
        public string? terminal { get; set; }

        public string? activo { get; set; }
        public string? estado { get; set; }
        public string? bandeja { get; set; }
        public string? usu_ins { get; set; }
        public string? fec_ins { get; set; }
        public string? usu_upd { get; set; }
        public string? fec_upd { get; set; }

    }
}
