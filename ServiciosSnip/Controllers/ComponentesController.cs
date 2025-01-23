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
    public class ComponentesController : Controller
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogService _logService;
        private readonly string _route;

        public ComponentesController(IDbConnection dbConnection, ILogService logService)
        {
            _dbConnection = dbConnection;
            _logService = logService;
            _route = "servicios/v1/snip/cla/componentes";
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> componentes([FromQuery] int? id_cla_componente = null)
        {
            var fechaHora = DateTime.Now;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            var query = "SELECT * FROM cla_componentes WHERE 1=1";
            var parameters = new DynamicParameters();

            if (id_cla_componente.HasValue)
            {
                query += " AND id_clacomponente = @id_cla_componente";
                parameters.Add("id_cla_componente", id_cla_componente.Value);
            }

            var componentes = await _dbConnection.QueryAsync(query, parameters);

            var result = new List<object>();
            foreach (var componente in componentes)
            {
                result.Add(new
                {
                    id_cla_componente = componente.id_clacomponente,
                    id_tipologia = componente.id_tipologias,
                    des_componente = componente.descripcion,
                    flg_habilitado = componente.activo == "S"
                });
            }

            var totalRegistros = result.Count;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            SentrySdk.CaptureMessage($"Usuario: {userName} Consulta el endpoint de Topologias a las {DateTime.Now}, desde ip: {ipAddress}");
            await _logService.LogAsync("Info", $"Usuario: {userName} Consulta el endpoint de Topologias a las {DateTime.Now}", int.Parse(userId), ipAddress, _route, $"id_cla_componente: {id_cla_componente}", JsonConvert.SerializeObject(result), "GET");

            var response = new
            {
                total_registros = totalRegistros,
                cla_componentes = result
            };

            return Ok(response);
        }
    }
}
