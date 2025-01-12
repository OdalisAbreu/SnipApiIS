using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Dapper;
using System.Security.Claims;
using ServiciosSnip.Services;

namespace SnipAuthServerV1.Controllers
{
    [ApiController]
    [Route("servicios/v1/snip/cla/[controller]")]
    public class InstitucionesController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<InstitucionesController> _logger;
        private readonly ILogService _logService;

        public InstitucionesController(IConfiguration configuration, ILogger<InstitucionesController> logger, ILogService logService)
        {
            _configuration = configuration;
            _logger = logger;
            _logService = logService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetInstituciones(
            [FromQuery] int? id_institucion = null,
            [FromQuery] string? cod_inst_snip = null,
            [FromQuery] string? cod_capitulo = null,
            [FromQuery] string? cod_subcapitulo = null,
            [FromQuery] string? cod_ue = null)
        {
            // Obtener la hora actual y el nombre del usuario autenticado
            var fechaHora = DateTime.UtcNow; // Hora en UTC
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            // Validación de campos requeridos
            if ((cod_capitulo != null || cod_subcapitulo != null || cod_ue != null) &&
                (cod_capitulo == null || cod_subcapitulo == null || cod_ue == null))
            {
                var missingFields = new List<string>();
                if (cod_capitulo == null) missingFields.Add(nameof(cod_capitulo));
                if (cod_subcapitulo == null) missingFields.Add(nameof(cod_subcapitulo));
                if (cod_ue == null) missingFields.Add(nameof(cod_ue));

                return BadRequest(new { Message = $"Faltan los siguientes campos requeridos: {string.Join(", ", missingFields)}" });
            }

            // Crear conexión a la base de datos
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            // Construir la consulta SQL dinámica
            var query = "SELECT * FROM cla_instituciones_snip WHERE 1=1";
            var parameters = new DynamicParameters();

            if (id_institucion.HasValue)
            {
                query += " AND id_institucion = @id_institucion";
                parameters.Add("id_institucion", id_institucion.Value);
            }

            if (!string.IsNullOrEmpty(cod_inst_snip))
            {
                query += " AND sigla = @cod_inst_snip";
                parameters.Add("cod_inst_snip", cod_inst_snip);
            }

            if (!string.IsNullOrEmpty(cod_capitulo) && !string.IsNullOrEmpty(cod_subcapitulo) && !string.IsNullOrEmpty(cod_ue))
            {
                query += " AND capitulo = @cod_capitulo AND subcapitulo = @cod_subcapitulo AND ue = @cod_ue";
                parameters.Add("cod_capitulo", cod_capitulo);
                parameters.Add("cod_subcapitulo", cod_subcapitulo);
                parameters.Add("cod_ue", cod_ue);
            }

            // Ejecutar la consulta
            var instituciones = await connection.QueryAsync(query, parameters);

            // Transformar los datos al formato requerido
            var result = new List<object>();
            foreach (var institucion in instituciones)
            {
                result.Add(new
                {
                    id_institucion = institucion.id_institucion,
                    des_institucion = institucion.descripcion,
                    siglas_institucion = institucion.sigla,
                    ins_cod_capitulo = institucion.capitulo,
                    ins_cod_subcapitulo = institucion.subcapitulo,
                    ins_cod_ue = institucion.ue,
                    eje_cod_capitulo = institucion.capitulo,
                    eje_cod_subcapitulo = institucion.subcapitulo,
                    eje_cod_ue = institucion.ue,
                    flg_habilitado = institucion.activo == "S"
                });
            }

            // Obtener el total de registros
            var totalRegistros = result.Count;

            // Obtener la dirección IP del usuario
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            SentrySdk.CaptureMessage($"Consulta Usuario: {userName} al endpoint GetInstituciones desde IP {ipAddress} a las {DateTime.UtcNow}");
            await _logService.LogAsync("Info", $"Consulta Usuario: {userName} al endpoint GetInstituciones desde IP {ipAddress} a las {DateTime.UtcNow}", int.Parse(userId));

            var objet = new List<object>();
            objet.Add(new
            {
                total_registros = totalRegistros,
                cla_instituciones = new List<object>(result),
            });
            return Ok(objet[0]);
        }
    }
}
