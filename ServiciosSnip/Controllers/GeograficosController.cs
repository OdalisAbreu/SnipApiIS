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
    public class GeograficosController : Controller
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogService _logService;
        private readonly string _route;

        public GeograficosController(IDbConnection dbConnection, ILogService logService)
        {
            _dbConnection = dbConnection;
            _logService = logService;
            _route = "servicios/v1/snip/cla/geografico";
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> geograficos([FromQuery] int? id_geografico = null)
        {
            var fechaHora = DateTime.Now;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            var query = "SELECT * FROM vw_cla_geografico WHERE 1=1";
            var parameters = new DynamicParameters();

            if (id_geografico.HasValue)
            {
                query += " AND id_clageo = @id_geografico";
                parameters.Add("id_geografico", id_geografico.Value);
            }

            var geograficos = await _dbConnection.QueryAsync(query, parameters);

            var result = new List<object>();
            foreach (var geografico in geograficos)
            {
                result.Add(new
                {
                    id_geografico = geografico.id_clageo,
                    des_geografico = geografico.descripcion,
                    id_area_influencia = geografico.area_influencia,
                    des_area_influencia = geografico.area_influencia_txt,
                    cod_provincia = geografico.cod_provincia,
                    cod_municipio = geografico.cod_municipio,
                    flg_habilitado = geografico.activo == "S"
                });
            }

            var totalRegistros = result.Count;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            SentrySdk.CaptureMessage($"Consulta Usuario: {userName} al endpoint Topologias a las {DateTime.Now}, desde IP {ipAddress}");
            await _logService.LogAsync("Info", $"Consulta Usuario: {userName} al endpoint Topologias a las {DateTime.Now}, desde IP {ipAddress}", int.Parse(userId), ipAddress, _route, $"id_geografico: {id_geografico}", JsonConvert.SerializeObject(result), "GET");

            var response = new
            {
                total_registros = totalRegistros,
                cla_geograficos = result
            };

            return Ok(response);
        }
    }
}
