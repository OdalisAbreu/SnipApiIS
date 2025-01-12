using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiciosSnip.Services;
using System.Data.SqlClient;
using System.Security.Claims;

namespace SnipAuthServerV1.Controllers
{
    [ApiController]
    [Route("servicios/v1/snip/cla/[controller]")]
    public class ComponentesController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogService _logService;

        public ComponentesController(IConfiguration configuration, ILogService logService)
        {
            _configuration = configuration;
            _logService = logService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> componentes([FromQuery] int? id_cla_componente = null)
        {
            var fechaHora = DateTime.Now;

            //var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

          //  return Ok(new { userId, userName, claims });

            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            var query = "SELECT * FROM cla_componentes WHERE 1=1";
            var parameters = new DynamicParameters();


            if (id_cla_componente.HasValue)
            {
                query += "AND id_clacomponente = @id_cla_componente";
                parameters.Add("id_cla_componente", id_cla_componente.Value);
            }

            var componentes = await connection.QueryAsync(query, parameters);

            var result = new List<object>();
            foreach (var componente in componentes)
            {
                result.Add(new
                {
                    id_cla_componente = componente.id_clacomponente,
                    id_tipologia = componente.id_tipologias,
                    des_componente = componente.descripcion,
                    flg_habilitado = componente.activo == "S" ? true : false
                });
            }
            var totalRegistros = result.Count;
            // Obtener la dirección IP del usuario
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            SentrySdk.CaptureMessage($"Usuario: {userName} Consulta el endpoint de Topologias a las {DateTime.Now}, desde ip: {ipAddress}");
            await _logService.LogAsync("Info", $"Usuario: {userName} Consulta el endpoint de Topologias a las {DateTime.Now}, desde ip: {ipAddress}", int.Parse(userId));
            
            var objet = new List<object>();
            objet.Add(new
            {
                total_registros = totalRegistros,
                cla_componentes = new List<object>(result)
            });            
            return Ok(objet[0]);
        }
    }
}
