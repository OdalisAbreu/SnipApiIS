using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace SigefApi.Controllers
{
    [ApiController]
    [Route("api/clasificadores/sigeft/FuentesDeFinanciamiento")]
    [Authorize] // Requiere token Bearer
    public class ClasificadoresController : ControllerBase
    {
        // Data Dumi para clasificador presupuestario
        private static readonly List<ClasificadorPresupuestario> Clasificadores = new()
        {
            new ClasificadorPresupuestario
            {
                CodGrupo = "1",
                DescripcionGrupo = "Fuente Internas",
                CodFuente = "10",
                DescripcionFuente = "Fuente General",
                CodFuenteEspecifica = "010",
                DescripcionFuenteEspecifica = "Fuente Especifica",
                Estado = "habilitado",
                Condicion = "vigente"
            },
            new ClasificadorPresupuestario
            {
                CodGrupo = "2",
                DescripcionGrupo = "Fuente Externas",
                CodFuente = "20",
                DescripcionFuente = "Fuente de Donaciones",
                CodFuenteEspecifica = "020",
                DescripcionFuenteEspecifica = "Donaciones Especificas",
                Estado = "habilitado",
                Condicion = "vigente"
            },
            new ClasificadorPresupuestario
            {
                CodGrupo = "3",
                DescripcionGrupo = "Fuente Externas",
                CodFuente = "30",
                DescripcionFuente = "Préstamos Externos",
                CodFuenteEspecifica = "030",
                DescripcionFuenteEspecifica = "Préstamos Multilaterales",
                Estado = "deshabilitado",
                Condicion = "no vigente"
            }
        };

        // GET ALL con paginación y filtros
        [HttpGet]
        public IActionResult GetClasificadores(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 10,
            [FromQuery] string codFteGral = null,
            [FromQuery] string estado = "vigente")
        {
            // Filtrar por cod_fte_gral y estado
            var query = Clasificadores.AsQueryable();
            if (!string.IsNullOrEmpty(codFteGral))
            {
                query = query.Where(c => c.CodFuente == codFteGral);
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

        // GET específico por código de fuente general
        [HttpGet("{cod_fte_gral}")]
        public IActionResult GetClasificadorPorCodigo(string cod_fte_gral)
        {
            var clasificador = Clasificadores
                .FirstOrDefault(c => c.CodFuente == cod_fte_gral && c.Estado == "habilitado" && c.Condicion == "vigente");

            if (clasificador == null)
            {
                return NotFound(new { mensaje = "Clasificador no encontrado o no vigente." });
            }

            return Ok(clasificador);
        }
    }

    // Modelo para el clasificador presupuestario
    public class ClasificadorPresupuestario
    {
        public string CodGrupo { get; set; }
        public string DescripcionGrupo { get; set; }
        public string CodFuente { get; set; }
        public string DescripcionFuente { get; set; }
        public string CodFuenteEspecifica { get; set; }
        public string DescripcionFuenteEspecifica { get; set; }
        public string Estado { get; set; }
        public string Condicion { get; set; }
    }
}