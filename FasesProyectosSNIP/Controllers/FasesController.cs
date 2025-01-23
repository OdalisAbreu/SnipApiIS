using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using FasesProyectosSNIP.Services;
using System.Data;
using System.Security.Claims;

namespace FasesProyectosSNIP.Controllers
{
    [ApiController]
    [Route("servicios/v1/snip/cla/[controller]")]
    public class FasesController : Controller
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogService _logService;
        private readonly string _route;

        public FasesController(IDbConnection dbConnection, ILogService logService)
        {
            _dbConnection = dbConnection;
            _logService = logService;
            _route = "servicios/v1/snip/cla/fases";
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ObtenerFases()
        {
            var fechaHora = DateTime.Now;

            // Obtener información del usuario autenticado
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            var query = @"
                SELECT 
                    dominio_sec AS id_fase, 
                    dominio_sig AS des_fase, 
                    CASE WHEN activo = 'S' THEN 1 ELSE 0 END AS flg_habilitado 
                FROM [dbo].[refcodes]
                WHERE dominio = 'fichas_proyectos.fase_actual' AND activo = 'S'";

            var fases = await _dbConnection.QueryAsync(query);

            // Formatear resultados
            var result = fases.Select(f => new
            {
                id_fase = f.id_fase,
                des_fase = f.des_fase,
                flg_habilitado = f.flg_habilitado == 1
            }).ToList();

            var totalRegistros = result.Count;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Registrar logs
         //  SentrySdk.CaptureMessage($"Usuario: {userName} Consultó el endpoint de Fases de Proyectos a las {fechaHora}, desde IP: {ipAddress}");
            await _logService.LogAsync("Info", $"Usuario: {userName} Consultó el endpoint de Fases de Proyectos a las {fechaHora}", int.Parse(userId), ipAddress, _route, "", JsonConvert.SerializeObject(result), "GET");

            var response = new
            {
                total_registros = totalRegistros,
                cla_fases = result
            };

            return Ok(response);
        }
    }
}