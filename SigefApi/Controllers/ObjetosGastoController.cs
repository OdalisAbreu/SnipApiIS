using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace SigefApi.Controllers
{
    [ApiController]
    [Route("api/clasificadores/sigeft/ObjetosGasto")]
    [Authorize] // Requiere token Bearer
    public class ObjetosGastoController : ControllerBase
    {
        // Data Dumi para clasificador de objetos del gasto
        private static readonly List<ObjetoGasto> Clasificadores = new()
        {
            new ObjetoGasto
            {
                cod_tipo = "2",
                descripcion_servicios_personales = "Gastos",
                cod_objeto = "1",
                descripcion_objeto = "Materiales y Suministros",
                cod_cuenta = "1",
                descripcion_cuenta = "Productos de papel, cartón e impresos",
                cod_sub_cuenta = "1",
                descripcion_sub_cuenta = "Libros, Revistas y Periódicos",
                cod_auxiliar = "15",
                descripcion_auxiliar = "Libros, revistas y periódicos",
                estado = "habilitado",
                condicion = "vigente"
            },
            new ObjetoGasto
            {
                cod_tipo = "2",
                descripcion_servicios_personales = "Gastos",
                cod_objeto = "1",
                descripcion_objeto = "Servicios Generales",
                cod_cuenta = "1",
                descripcion_cuenta = "Mantenimiento y Reparaciones",
                cod_sub_cuenta = "1",
                descripcion_sub_cuenta = "Mantenimiento de edificios",
                cod_auxiliar = "16",
                descripcion_auxiliar = "Reparaciones menores",
                estado = "habilitado",
                condicion = "vigente"
            },
            new ObjetoGasto
            {
                cod_tipo = "2",
                descripcion_servicios_personales = "Gastos",
                cod_objeto = "1",
                descripcion_objeto = "Equipo Menor",
                cod_cuenta = "1",
                descripcion_cuenta = "Equipo de oficina",
                cod_sub_cuenta = "1",
                descripcion_sub_cuenta = "Equipo de oficina no inventariable",
                cod_auxiliar = "17",
                descripcion_auxiliar = "Artículos de oficina menores",
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

        // GET específico por código de objeto
        [HttpGet("{cod_objetal}")]
        public IActionResult GetClasificadorPorCodigo(string cod_objetal)
        {
            if (string.IsNullOrEmpty(cod_objetal) || cod_objetal.Length != 6)
            {
                return BadRequest(new { mensaje = "El parámetro cod_fte_gral debe tener 6 caracteres." });
            }

            var cod_tipo = cod_objetal.Substring(0, 1);
            var cod_objeto = cod_objetal.Substring(1, 1);
            var cod_cuenta = cod_objetal.Substring(2, 1);
            var cod_sub_cuenta = cod_objetal.Substring(3, 1);
            var cod_auxiliar = cod_objetal.Substring(4, 2);

            var clasificador = Clasificadores.FirstOrDefault(c =>
                c.cod_tipo == cod_tipo &&
                c.cod_objeto == cod_objeto &&
                c.cod_cuenta == cod_cuenta &&
                c.cod_sub_cuenta == cod_sub_cuenta &&
                c.cod_auxiliar == cod_auxiliar &&
                c.estado == "habilitado" &&
                c.condicion == "vigente");


            if (clasificador == null)
            {
                return NotFound(new { mensaje = "Clasificador no encontrado o no vigente." });
            }

            return Ok(clasificador);
        }
    }

    // Modelo para clasificador de objetos del gasto
    public class ObjetoGasto
    {
        public string cod_tipo { get; set; }
        public string descripcion_servicios_personales { get; set; }
        public string cod_objeto { get; set; }
        public string descripcion_objeto { get; set; }
        public string cod_cuenta { get; set; }
        public string descripcion_cuenta { get; set; }
        public string cod_sub_cuenta { get; set; }
        public string descripcion_sub_cuenta { get; set; }
        public string cod_auxiliar { get; set; }
        public string descripcion_auxiliar { get; set; }
        public string estado { get; set; }
        public string condicion { get; set; }
    }
}