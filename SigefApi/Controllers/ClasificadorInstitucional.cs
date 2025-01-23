﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SigefApi.Controllers
{
    [ApiController]
    [Route("api/clasificadores/institucional")]
    [Authorize] // Requiere token Bearer
    public class ClasificadorInstitucionalController : ControllerBase
    {
        // Data dumi para Clasificador Institucional
        private static readonly List<ClasificadorInstitucional> Clasificadores = new()
        {
            new ClasificadorInstitucional
            {
                id_institucion = "1",
                desc_institucion = "SENADO DE LA REPÚBLICA DOMINICANA",
                sigla = "CONGRESO",
                cod_sector = "1",
                cod_subsector = "1",
                cod_area = "1",
                cod_subarea = "1",
                cod_seccion = "1",
                cod_poderes_oe = "01",
                cod_entidades = "0001",
                cod_capitulo = "0101",
                cod_subcapitulo = "01",
                cod_ue = "0001",
                estado = "habilitado",
                condicion = "vigente"
            },
            new ClasificadorInstitucional
            {
                id_institucion = "2",
                desc_institucion = "CÁMARA DE DIPUTADOS",
                sigla = "DIPUTADOS",
                cod_sector = "2",
                cod_subsector = "2",
                cod_area = "2",
                cod_subarea = "2",
                cod_seccion = "2",
                cod_poderes_oe = "02",
                cod_entidades = "0002",
                cod_capitulo = "0201",
                cod_subcapitulo = "02",
                cod_ue = "0002",
                estado = "habilitado",
                condicion = "vigente"
            }
        };

        // GET todos los clasificadores o filtrados por estado
        [HttpGet]
        public IActionResult GetAllClasificadores(
            [FromQuery] string estado = "vigente",
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 10)
        {
            try
            {
                var query = Clasificadores.AsQueryable();

                // Filtrar por estado
                if (!string.IsNullOrEmpty(estado))
                {
                    query = query.Where(c => c.estado == "habilitado" && c.condicion == estado);
                }

                var totalRegistros = query.Count();

                // Paginación
                var datos = query
                    .Skip((pagina - 1) * tamanoPagina)
                    .Take(tamanoPagina)
                    .ToList();

                return Ok(new
                {
                    totalRegistros,
                    paginaActual = pagina,
                    tamanoPagina,
                    datos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Ocurrió un error interno.", detalle = ex.Message });
            }
        }

        // GET detalle de una institución por id
        [HttpGet("{cod_institucion}")]
        public IActionResult GetClasificadorDetails(string cod_institucion)
        {
            try
            {
                if (string.IsNullOrEmpty(cod_institucion))
                {
                    return BadRequest(new { mensaje = "El parámetro id_institucion es requerido." });
                }

                var cod_sector = cod_institucion.Substring(0, 1);
                var cod_subsector = cod_institucion.Substring(1, 1);
                var cod_area = cod_institucion.Substring(2, 1);
                var cod_subarea = cod_institucion.Substring(3, 1);
                var cod_seccion = cod_institucion.Substring(4, 1);
                var cod_poderes_oe = cod_institucion.Substring(5, 2);
                var cod_entidades = cod_institucion.Substring(7, 4);
                var cod_capitulo = cod_institucion.Substring(11, 4);
                var cod_subcapitulo = cod_institucion.Substring(15, 2);
                var cod_ue = cod_institucion.Substring(17, 4);

                Console.WriteLine($"{cod_sector} - {cod_subsector}  - {cod_area} - {cod_subarea} - {cod_seccion} - {cod_poderes_oe}  - {cod_entidades}" +
                                    $" - {cod_capitulo} - {cod_subcapitulo} - {cod_ue} " );

                var clasificador = Clasificadores.FirstOrDefault(c =>
                    c.cod_sector == cod_sector &&
                    c.cod_subsector == cod_subsector &&
                    c.cod_area == cod_area &&
                    c.cod_subarea == cod_subarea &&
                    c.cod_seccion == cod_seccion &&
                    c.cod_poderes_oe == cod_poderes_oe &&
                    c.cod_entidades == cod_entidades &&
                    c.cod_capitulo == cod_capitulo &&
                    c.cod_subcapitulo == cod_subcapitulo &&
                    c.cod_sector == cod_sector &&
                    c.estado == "habilitado" &&
                    c.condicion == "vigente");

                if (clasificador == null)
                {
                    return NotFound(new { mensaje = "Clasificador no encontrado o no vigente." });
                }

                return Ok(clasificador);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Ocurrió un error interno.", detalle = ex.Message });
            }
        }
    }

    // Modelo para clasificador institucional
    public class ClasificadorInstitucional
    {
        public string id_institucion { get; set; }
        public string desc_institucion { get; set; }
        public string sigla { get; set; }
        public string cod_sector { get; set; }
        public string cod_subsector { get; set; }
        public string cod_area { get; set; }
        public string cod_subarea { get; set; }
        public string cod_seccion { get; set; }
        public string cod_poderes_oe { get; set; }
        public string cod_entidades { get; set; }
        public string cod_capitulo { get; set; }
        public string cod_subcapitulo { get; set; }
        public string cod_ue { get; set; }
        public string estado { get; set; }
        public string condicion { get; set; }
    }
}
