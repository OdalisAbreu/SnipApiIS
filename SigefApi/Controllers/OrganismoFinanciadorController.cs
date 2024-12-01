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
                CodGrupo = "1",
                DescripcionGrupo = "Organismos internos",
                CodSubGrupo = "2",
                DescripcionSubGrupo = "Otros organismos internos",
                CodOrgFin = "102",
                DescripcionOrgFin = "Fondos Propios",
                Estado = "habilitado",
                Condicion = "vigente"
            },
            new OrganismoFinanciador
            {
                CodGrupo = "1",
                DescripcionGrupo = "Organismos externos",
                CodSubGrupo = "3",
                DescripcionSubGrupo = "Organismos internacionales",
                CodOrgFin = "201",
                DescripcionOrgFin = "Préstamos Externos",
                Estado = "habilitado",
                Condicion = "vigente"
            },
            new OrganismoFinanciador
            {
                CodGrupo = "2",
                DescripcionGrupo = "Organismos internos",
                CodSubGrupo = "4",
                DescripcionSubGrupo = "Organismos descentralizados",
                CodOrgFin = "301",
                DescripcionOrgFin = "Transferencias Internas",
                Estado = "deshabilitado",
                Condicion = "no vigente"
            }
        };

        // GET ALL con paginación y filtros
        [HttpGet]
        public IActionResult GetClasificadores(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 10,
            [FromQuery] string Idcod_orgfin = null,
            [FromQuery] string estado = "vigente")
        {
            // Filtrar por código de organismo financiador y estado
            var query = Clasificadores.AsQueryable();
            if (!string.IsNullOrEmpty(Idcod_orgfin))
            {
                query = query.Where(c => c.CodOrgFin == Idcod_orgfin);
            }
            if (!string.IsNullOrEmpty(estado))
            {
                query = query.Where(c => c.Condicion == estado && c.Estado == "habilitado");
            }

            // Aplicar paginación
            var totalRegistros = query.Count();
            var resultado = query
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToList();

            return Ok(new
            {
                totalRegistros,
                pagina,
                tamanoPagina,
                datos = resultado
            });
        }

        // GET específico por código de organismo financiador
        [HttpGet("{Idcod_orgfin}")]
        public IActionResult GetClasificadorPorCodigo(string Idcod_orgfin)
        {
            var clasificador = Clasificadores
                .FirstOrDefault(c => c.CodOrgFin == Idcod_orgfin && c.Estado == "habilitado" && c.Condicion == "vigente");

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
        public string CodGrupo { get; set; }
        public string DescripcionGrupo { get; set; }
        public string CodSubGrupo { get; set; }
        public string DescripcionSubGrupo { get; set; }
        public string CodOrgFin { get; set; }
        public string DescripcionOrgFin { get; set; }
        public string Estado { get; set; }
        public string Condicion { get; set; }
    }
}