using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ServiciosSnip.Services;
using System.Data;
using System.Security.Claims;

namespace SnipAuthServerV1.Controllers
{
    [ApiController]
    [Route("servicios/v1/snip/cla/[controller]")]
    public class TipologiasController : Controller
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<TipologiasController> _logger;
        private readonly ILogService _logService;
        private readonly string _route;

        public TipologiasController(IDbConnection dbConnection, ILogger<TipologiasController> logger, ILogService logService)
        {
            _dbConnection = dbConnection;
            _logger = logger;
            _logService = logService;
            _route = "servicios/v1/snip/cla/tipologias";
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> tipologias([FromQuery] int? id_tipologia = null)
        {
            var fechaHora = DateTime.UtcNow; // Hora en UTC
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            var query = "SELECT * FROM cla_tipologias WHERE 1=1";
            var parameters = new DynamicParameters();

            if (id_tipologia.HasValue)
            {
                query += " AND id_tipologia = @id_tipologia";
                parameters.Add("id_tipologia", id_tipologia.Value);
            }

            var tipologias = await _dbConnection.QueryAsync(query, parameters);

            var result = new List<object>();
            foreach (var tipologia in tipologias)
            {
                result.Add(new
                {
                    id_tipologia = tipologia.id_tipologia,
                    des_tipologia = tipologia.descripcion,
                    flg_habilitado = tipologia.usu_ins == 1
                });
            }

            var totalRegistros = result.Count;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            SentrySdk.CaptureMessage($"Consulta Usuario: {userName} al endpoint Topologias a las {DateTime.Now}, desde IP: {ipAddress}");
            await _logService.LogAsync("Info", $"Consulta Usuario: {userName} al endpoint Topologias a las {DateTime.Now}, desde IP: {ipAddress}", int.Parse(userId), ipAddress, _route, $"id_tipologia: {id_tipologia}", JsonConvert.SerializeObject(result), "GET");

            var response = new
            {
                total_registros = totalRegistros,
                cla_tipologias = result
            };

            return Ok(response);
        }
    }
}
