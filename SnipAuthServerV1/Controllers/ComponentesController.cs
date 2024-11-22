using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace SnipAuthServerV1.Controllers
{
    [ApiController]
    [Route("servicios/v1/snip/cla/[controller]")]
    public class ComponentesController : Controller
    {
        private readonly IConfiguration _configuration;
     //   private readonly ILogger _logger;

        public ComponentesController(IConfiguration configuration/*, ILogger logger*/)
        {
            _configuration = configuration;
       //     _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> componentes([FromQuery] int? id_cla_componente = null)
        {
            var fechaHora = DateTime.Now;
            var usuario = User.Identity?.Name ?? "Usuario anónimo";

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

            SentrySdk.CaptureMessage($"Consulta el endpoint Topologias a las {DateTime.Now}");
            
            var objet = new List<object>();
            objet.Add(new
            {
                cla_componentes = new List<object>(result),
            });            
            return Ok(objet[0]);
            
            //return Ok(result);
        }
    }
}
