using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace SigefApi.Controllers
{
    [ApiController]
    [Route("api/clasificadores/sigeft/FinalidadFuncion")]
    [Authorize] // Requiere token Bearer
    public class FinalidadFuncionController : ControllerBase
    {
        // Data Dumi para clasificador de finalidad y función
        private static readonly List<FinalidadFuncion> Clasificadores = new()
        {
            new FinalidadFuncion
            {
                CodFinalidad = "1",
                DescripcionFinalidad = "Servicio Público Generales",
                CodFuncion = "11",
                DescripcionFuncion = "Administración General",
                CodSubFuncion = "1101",
                DescripcionSubFuncion = "Órganos Ejecutivos y Legislativos",
                Estado = "habilitado",
                Condicion = "vigente"
            },
            new FinalidadFuncion
            {
                CodFinalidad = "2",
                DescripcionFinalidad = "Defensa y Seguridad",
                CodFuncion = "21",
                DescripcionFuncion = "Seguridad Pública",
                CodSubFuncion = "2101",
                DescripcionSubFuncion = "Defensa Nacional",
                Estado = "habilitado",
                Condicion = "vigente"
            },
            new FinalidadFuncion
            {
                CodFinalidad = "3",
                DescripcionFinalidad = "Educación",
                CodFuncion = "31",
                DescripcionFuncion = "Servicios Educativos",
                CodSubFuncion = "3101",
                DescripcionSubFuncion = "Educación Básica",
                Estado = "deshabilitado",
                Condicion = "no vigente"
            }
        };

        // GET ALL con paginación y filtros
        [HttpGet]
        public IActionResult GetClasificadores(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 10,
            [FromQuery] string codSuFuncion = null,
            [FromQuery] string estado = "vigente")
        {
            // Filtrar por código de sub-función y estado
            var query = Clasificadores.AsQueryable();
            if (!string.IsNullOrEmpty(codSuFuncion))
            {
                query = query.Where(c => c.CodSubFuncion == codSuFuncion);
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

        // GET específico por código de sub-función
        [HttpGet("{cod_su_funcion}")]
        public IActionResult GetClasificadorPorCodigo(string cod_su_funcion)
        {
            var clasificador = Clasificadores
                .FirstOrDefault(c => c.CodSubFuncion == cod_su_funcion && c.Estado == "habilitado" && c.Condicion == "vigente");

            if (clasificador == null)
            {
                return NotFound(new { mensaje = "Clasificador no encontrado o no vigente." });
            }

            return Ok(clasificador);
        }
    }

    // Modelo para clasificador de finalidad y función
    public class FinalidadFuncion
    {
        public string CodFinalidad { get; set; }
        public string DescripcionFinalidad { get; set; }
        public string CodFuncion { get; set; }
        public string DescripcionFuncion { get; set; }
        public string CodSubFuncion { get; set; }
        public string DescripcionSubFuncion { get; set; }
        public string Estado { get; set; }
        public string Condicion { get; set; }
    }
}