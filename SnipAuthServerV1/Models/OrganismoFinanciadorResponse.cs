namespace SnipAuthServerV1.Models
{
    public class OrganismoFinanciadorResponse
    {
        public string cod_grupo { get; set; }
        public string descripcion_grupo { get; set; }
        public string cod_sub_grupo { get; set; }
        public string descripcion_subgrupo { get; set; }
        public string cod_org_fin { get; set; }
        public string descripcion_org_fin { get; set; }
        public string estado { get; set; }
        public string condicion { get; set; }
    }

    public class OrganismosFinanciadoresResponsive
    {
        public List<OrganismoFinanciadorResponse>? datos { get; set; }
    }

}
