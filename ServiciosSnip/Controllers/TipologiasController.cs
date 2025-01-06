using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Security.Claims;

namespace SnipAuthServerV1.Controllers
{
    [ApiController]
    [Route("servicios/v1/snip/cla/[controller]")]
    public class TipologiasController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TipologiasController> _logger;

        public TipologiasController(IConfiguration configuration, ILogger<TipologiasController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> tipologias([FromQuery] int? id_tipologia = null  )
        {
            // Obtener la hora actual y el nombre del usuario autenticado
            var fechaHora = DateTime.UtcNow; // Hora en UTC
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            var query = "SELECT * FROM cla_tipologias WHERE 1=1";
            var parameters = new DynamicParameters();


            if (id_tipologia.HasValue)
            {
                query += "AND id_tipologia = @id_tipologia";
                parameters.Add("id_tipologia", id_tipologia.Value);
            }

            var tipologias = await connection.QueryAsync(query, parameters);

            var result = new List<object>();
            foreach (var tipologia in tipologias) 
            {
                result.Add(new {
                    id_tipologia = tipologia.id_tipologia,
                    des_tipologia = tipologia.descripcion,
                    flg_habilitado = tipologia.usu_ins == 1 ? true : false
                });
            }
            var totalRegistros = result.Count;
            // Obtener la dirección IP del usuario
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            SentrySdk.CaptureMessage($"Consulta Usuario: {userName} al endpoint Topologias a las {DateTime.Now}, desde IP: {ipAddress}");

            var objet = new List<object>();
            objet.Add(new
            {
                total_registros = totalRegistros,
                cla_tipologias = new List<object>(result),
            });
            return Ok(objet[0]);
        }

    }
}
