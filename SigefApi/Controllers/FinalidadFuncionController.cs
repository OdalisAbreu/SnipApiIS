﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SigefApi.Controllers
{
    [ApiController]
    [Route("api/clasificadores/sigeft/fuente")]
    [Authorize] // Requiere token Bearer
    public class FuenteController : ControllerBase
    {
        // Data Dumi para fuentes
        private static readonly List<Fuente> Clasificadores = new()
        {
            new Fuente
            {
                cod_finalidad = "1",
                descripcion_finalidad = "Fuente Internas",
                cod_funcion = "10",
                descripcion_funcion = "Fuente General",
                cod_sub_funcion = "0100",
                descripcion_sub_funcion = "Fuente Específica 1",
                estado = "habilitado",
                condicion = "vigente"
            },
            new Fuente
            {
                cod_finalidad = "1",
                descripcion_finalidad = "Fuente Internas",
                cod_funcion = "20",
                descripcion_funcion = "Fuente Especial",
                cod_sub_funcion = "0200",
                descripcion_sub_funcion = "Fuente Específica 2",
                estado = "habilitado",
                condicion = "vigente"
            },
            new Fuente
            {
                cod_finalidad = "2",
                descripcion_finalidad = "Fuente Externas",
                cod_funcion = "30",
                descripcion_funcion = "Fuente de Cooperación",
                cod_sub_funcion = "0300",
                descripcion_sub_funcion = "Fuente Específica 3",
                estado = "habilitado",
                condicion = "vigente"
            }
        };

        // GET todos los clasificadores o filtrados por cod_fte_gral
        [HttpGet]
        public IActionResult GetAllClasificadores([FromQuery] string estado = "vigente")
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Ocurrió un error interno.", detalle = ex.Message });
            }
        }

        // GET con cod_fte_gral como parámetro
        [HttpGet("{cod_fte_gral}")]
        public IActionResult GetFuenteDetails(string cod_fte_gral)
        {
            try
            {
                // Validar longitud mínima de cod_fte_gral
                if (string.IsNullOrEmpty(cod_fte_gral) || cod_fte_gral.Length != 7)
                {
                    return BadRequest(new { mensaje = "El parámetro cod_fte_gral debe tener 7 caracteres." });
                }
                // Descomponer cod_fte_gral
                var cod_finalidad = cod_fte_gral.Substring(0, 1);
                var cod_funcion = cod_fte_gral.Substring(1, 2);
                var cod_sub_funcion = cod_fte_gral.Substring(3, 4);
            

                // Buscar en la data dumi
                var fuente = Clasificadores.FirstOrDefault(f =>
                    f.cod_finalidad == cod_finalidad &&
                    f.cod_funcion == cod_funcion &&
                    f.cod_sub_funcion == cod_sub_funcion &&
                    f.estado == "habilitado" &&
                    f.condicion == "vigente");

                if (fuente == null)
                {
                    return NotFound(new { mensaje = "Fuente no encontrada o no vigente." });
                }

                // Crear objeto de respuesta
                var resultado = new
                {
                    cod_finalidad,
                    fuente.descripcion_finalidad,
                    cod_funcion,
                    fuente.descripcion_funcion,
                    cod_sub_funcion,
                    fuente.descripcion_sub_funcion,
                    fuente.estado,
                    fuente.condicion
                };

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Ocurrió un error interno.", detalle = ex.Message });
            }
        }
    }

    // Modelo para clasificador de fuentes
    public class Fuente
    {
        public string cod_finalidad { get; set; }
        public string descripcion_finalidad { get; set; }
        public string cod_funcion { get; set; }
        public string descripcion_funcion { get; set; }
        public string cod_sub_funcion { get; set; }
        public string descripcion_sub_funcion { get; set; }
        public string estado { get; set; }
        public string condicion { get; set; }
    }
}