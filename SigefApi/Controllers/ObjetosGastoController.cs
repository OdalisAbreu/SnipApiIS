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
                CodTipo = "2",
                DescripcionServiciosPersonales = "Gastos",
                CodObjeto = "3",
                DescripcionObjeto = "Materiales y Suministros",
                CodCuenta = "3",
                DescripcionCuenta = "Productos de papel, cartón e impresos",
                CodSubCuenta = "4",
                DescripcionSubCuenta = "Libros, Revistas y Periódicos",
                CodAuxiliar = "01",
                DescripcionAuxiliar = "Libros, revistas y periódicos",
                Estado = "habilitado",
                Condicion = "vigente"
            },
            new ObjetoGasto
            {
                CodTipo = "2",
                DescripcionServiciosPersonales = "Gastos",
                CodObjeto = "4",
                DescripcionObjeto = "Servicios Generales",
                CodCuenta = "5",
                DescripcionCuenta = "Mantenimiento y Reparaciones",
                CodSubCuenta = "1",
                DescripcionSubCuenta = "Mantenimiento de edificios",
                CodAuxiliar = "01",
                DescripcionAuxiliar = "Reparaciones menores",
                Estado = "habilitado",
                Condicion = "vigente"
            },
            new ObjetoGasto
            {
                CodTipo = "2",
                DescripcionServiciosPersonales = "Gastos",
                CodObjeto = "6",
                DescripcionObjeto = "Equipo Menor",
                CodCuenta = "8",
                DescripcionCuenta = "Equipo de oficina",
                CodSubCuenta = "2",
                DescripcionSubCuenta = "Equipo de oficina no inventariable",
                CodAuxiliar = "03",
                DescripcionAuxiliar = "Artículos de oficina menores",
                Estado = "deshabilitado",
                Condicion = "no vigente"
            }
        };

        // GET ALL con paginación y filtros
        [HttpGet]
        public IActionResult GetClasificadores(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 10,
            [FromQuery] string codObjetal = null,
            [FromQuery] string estado = "vigente")
        {
            // Filtrar por código de objeto y estado
            var query = Clasificadores.AsQueryable();
            if (!string.IsNullOrEmpty(codObjetal))
            {
                query = query.Where(c => c.CodAuxiliar == codObjetal);
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

        // GET específico por código de objeto
        [HttpGet("{cod_objetal}")]
        public IActionResult GetClasificadorPorCodigo(string cod_objetal)
        {
            var clasificador = Clasificadores
                .FirstOrDefault(c => c.CodAuxiliar == cod_objetal && c.Estado == "habilitado" && c.Condicion == "vigente");

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
        public string CodTipo { get; set; }
        public string DescripcionServiciosPersonales { get; set; }
        public string CodObjeto { get; set; }
        public string DescripcionObjeto { get; set; }
        public string CodCuenta { get; set; }
        public string DescripcionCuenta { get; set; }
        public string CodSubCuenta { get; set; }
        public string DescripcionSubCuenta { get; set; }
        public string CodAuxiliar { get; set; }
        public string DescripcionAuxiliar { get; set; }
        public string Estado { get; set; }
        public string Condicion { get; set; }
    }
}