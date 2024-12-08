using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace SigefApi.Controllers
{
    [ApiController]
    [Route("api/clasificadores/sigeft/OrganismoFinanciador")]
    [Authorize] // Requiere token Bearer
    public class OrganismoFinanciadorController : ControllerBase
    {
        // Data Dumi para clasificador de organismos financiadores
        private static readonly List<OrganismoFinanciador> Clasificadores = new()
        {
            new OrganismoFinanciador
            {
                cod_grupo = "1",
                descripcion_grupo = "Organismos internos",
                cod_sub_grupo = "2",
                descripcion_subgrupo = "Otros organismos internos",
                cod_org_fin = "131",
                descripcion_org_fin = "Fondos Propios",
                estado = "habilitado",
                condicion = "vigente"
            },
            new OrganismoFinanciador
            {
                cod_grupo = "1",
                descripcion_grupo = "Organismos externos",
                cod_sub_grupo = "2",
                descripcion_subgrupo = "Organismos internacionales",
                cod_org_fin = "132",
                descripcion_org_fin = "Préstamos Externos",
                estado = "habilitado",
                condicion = "vigente"
            },
            new OrganismoFinanciador
            {
                cod_grupo = "1",
                descripcion_grupo = "Organismos internos",
                cod_sub_grupo = "2",
                descripcion_subgrupo = "Organismos descentralizados",
                cod_org_fin = "133",
                descripcion_org_fin = "Transferencias Internas",
                estado = "deshabilitado",
                condicion = "no vigente"
            }
        };

        // GET ALL con paginación y filtros
        [HttpGet]
        public IActionResult GetClasificadores(
            [FromQuery] string estado = "vigente")
        {
            // Filtrar por estado
            var query = Clasificadores.AsQueryable();

            if (!string.IsNullOrEmpty(estado))
            {
                query = query.Where(f => f.estado == "habilitado" && f.condicion == estado);
            }

            var resultado = query.ToList();

            return Ok(new
            {
                totalRegistros = resultado.Count,
                datos = resultado
            });
        }

        // GET específico por código de organismo financiador
        [HttpGet("{Idcod_orgfin}")]
        public IActionResult GetClasificadorPorCodigo(string Idcod_orgfin)
        {
            if (string.IsNullOrEmpty(Idcod_orgfin) || Idcod_orgfin.Length != 5)
            {
                return BadRequest(new { mensaje = "El parámetro cod_fte_gral debe tener 5 caracteres." });
            }

            var cod_grupo = Idcod_orgfin.Substring(0, 1);
            var cod_sub_grupo = Idcod_orgfin.Substring(1, 1);
            var cod_org_fin = Idcod_orgfin.Substring(2, 3);



            var clasificador = Clasificadores.FirstOrDefault(f =>
              f.cod_grupo == cod_grupo &&
              f.cod_sub_grupo == cod_sub_grupo &&
              f.cod_org_fin == cod_org_fin &&
              f.estado == "habilitado" &&
              f.condicion == "vigente");

            if (clasificador == null)
            {
                return NotFound(new { mensaje = "Clasificador no encontrado o no vigente." });
            }

            return Ok(clasificador);
        }
    }

    // Modelo para clasificador de organismos financiadores
    public class OrganismoFinanciador
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
}