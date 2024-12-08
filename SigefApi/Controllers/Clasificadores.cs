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
                cod_grupo = "1",
                descripcion_grupo = "Fuente Internas",
                cod_fuente = "10",
                descripcion_fuente = "Fuente General",
                cod_fuente_especifica = "0100",
                descripcion_fuente_especifica = "Fuente Especifica",
                estado = "habilitado",
                condicion = "vigente"
            },
            new ClasificadorPresupuestario
            {
                cod_grupo = "2",
                descripcion_grupo = "Fuente Externas",
                cod_fuente = "20",
                descripcion_fuente = "Fuente de Donaciones",
                cod_fuente_especifica = "0200",
                descripcion_fuente_especifica = "Donaciones Especificas",
                estado = "habilitado",
                condicion = "vigente"
            },
            new ClasificadorPresupuestario
            {
                cod_grupo = "3",
                descripcion_grupo = "Fuente Externas",
                cod_fuente = "30",
                descripcion_fuente = "Préstamos Externos",
                cod_fuente_especifica = "0300",
                descripcion_fuente_especifica = "Préstamos Multilaterales",
                estado = "habilitado",
                condicion = "vigente"
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

        // GET específico por código de fuente general
        [HttpGet("{cod_fte_gral}")]
        public IActionResult GetClasificadorPorCodigo(string cod_fte_gral)
        {
            if (string.IsNullOrEmpty(cod_fte_gral) || cod_fte_gral.Length != 7)
            {
                return BadRequest(new { mensaje = "El parámetro cod_fte_gral debe tener 7 caracteres." });
            }

            var cod_grupo = cod_fte_gral.Substring(0, 1);
            var cod_fuente = cod_fte_gral.Substring(1, 2);
            var cod_fuente_especifica = cod_fte_gral.Substring(3, 4);


        /*    var clasificador = Clasificadores
                .FirstOrDefault(
                c => c.CodFuente == cod_fte_gral && c.Estado == "habilitado" && c.Condicion == "vigente");*/
            var clasificador = Clasificadores.FirstOrDefault(f =>
                f.cod_grupo == cod_grupo &&
                f.cod_fuente == cod_fuente &&
                f.cod_fuente_especifica == cod_fuente_especifica &&
                f.estado == "habilitado" &&
                f.condicion == "vigente");

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
        public string cod_grupo { get; set; }
        public string descripcion_grupo { get; set; }
        public string cod_fuente { get; set; }
        public string descripcion_fuente { get; set; }
        public string cod_fuente_especifica { get; set; }
        public string descripcion_fuente_especifica { get; set; }
        public string estado { get; set; }
        public string condicion { get; set; }
    }
}