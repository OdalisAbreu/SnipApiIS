﻿using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using EstadoProyectosSNIP.Services;
using System.Data;
using System.Security.Claims;

namespace EstadoProyectosSNIP.Controllers
{
    [ApiController]
    [Route("servicios/v1/snip/cla/[controller]")]
    public class EstadosController : Controller
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogService _logService;
        private readonly string _route;

        public EstadosController(IDbConnection dbConnection, ILogService logService)
        {
            _dbConnection = dbConnection;
            _logService = logService;
            _route = "servicios/v1/snip/cla/estados";
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ObtenerEstados()
        {
            var fechaHora = DateTime.Now;

            // Obtener datos del usuario
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            var query = @"
                SELECT 
                    dominio_sec AS id_estado_proyecto, 
                    dominio_sig AS des_estado_proyecto, 
                    CASE WHEN activo = 'S' THEN 1 ELSE 0 END AS flg_habilitado 
                FROM [dbo].[refcodes]
                WHERE dominio = 'fichas_proyectos.estado_proyecto' AND activo = 'S'";

            var estados = await _dbConnection.QueryAsync(query);

            // Formatear los resultados
            var result = estados.Select(e => new
            {
                id_estado_proyecto = e.id_estado_proyecto,
                des_estado_proyecto = e.des_estado_proyecto,
                flg_habilitado = e.flg_habilitado == 1
            }).ToList();

            var totalRegistros = result.Count;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Loguear la consulta
          //  SentrySdk.CaptureMessage($"Usuario: {userName} Consulta el endpoint de Estados de Proyectos a las {fechaHora}, desde IP: {ipAddress}");
            await _logService.LogAsync("Info", $"Usuario: {userName} Consulta el endpoint de Estados de Proyectos a las {fechaHora}", int.Parse(userId), ipAddress, _route, "", JsonConvert.SerializeObject(result), "GET");

            var response = new
            {
                total_registros = totalRegistros,
                cla_estados_proyecto = result
            };

            return Ok(response);
        }
    }
}