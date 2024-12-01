using Dapper;
using Microsoft.AspNetCore.Mvc;
using SnipAuthServerV1.Models;
using SnipAuthServerV1.Services;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace SnipAuthServerV1.Controllers
{
    [ApiController]
    [Route("/servicios/v1/sigef/cla/financiamiento/fgeneral")]
    public class FuentesGeneralesSigefController : Controller
    {
        private readonly IDbConnection _dbConnection;
        private readonly ExternalApiService _externalApiService;

        public FuentesGeneralesSigefController(IDbConnection dbConnection, ExternalApiService externalApiService)
        {
            _dbConnection = dbConnection;
            _externalApiService = externalApiService;
        }

        [HttpPost]
        public async Task<IActionResult> InsertFuenteFromExternalService([FromBody] CodFteGralRequest request)
        {
            // Validar que el campo codFteGral sea obligatorio
            if (string.IsNullOrEmpty(request.CodFteGral))
            {
                return BadRequest(new
                {
                    estatus_code = "400",
                    estatus_msg = "El campo 'codFteGral' es obligatorio."
                });
            }

            // Autenticación: Obtener el token de acceso
            var token = await _externalApiService.GetAuthTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                return StatusCode(401, new
                {
                    estatus_code = "401",
                    estatus_msg = "No se pudo autenticar con el servicio externo."
                });
            }

            // Construir la URL con los parámetros requeridos
            string url = $"https://localhost:7261/api/clasificadores/sigeft/FuentesDeFinanciamiento?pagina=1&tamanoPagina=10&codFteGral={request.CodFteGral}&estado=vigente";

            // Consumir el servicio externo
            var fuentesExternas = await _externalApiService.GetFuentesExternasAsync(url, token);

            if (fuentesExternas == null || fuentesExternas.Datos.Count == 0)
            {
                return BadRequest(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontraron fuentes externas para insertar."
                });
            }

            // Seleccionar la primera fuente externa
            var fuenteExterna = fuentesExternas.Datos[0];

            // Insertar en la base de datos utilizando el procedimiento almacenado
            var result = _dbConnection.Execute("dbo.f_cla_fuentes_generales_ins", new
            {
                id_fte_gral = 0,
                id_version = 1,
                cod_fte_gral = request.CodFteGral,
                descripcion = fuenteExterna.DescripcionFuente,
                tipo_fuente = fuenteExterna.CodGrupo,
                activo = fuenteExterna.Estado == "habilitado" ? "S" : "N",
                estado = "registrar",
                bandeja = 0,
                usu_ins = 1,
                fec_ins = DateTime.Now
            }, commandType: CommandType.StoredProcedure);

            return Ok(new
            {
                estatus_code = "201",
                estatus_msg = "Fuente registrada correctamente a partir del servicio externo."
            });
        }
    }

    // Clase para recibir el JSON del cliente
    public class CodFteGralRequest
    {
        public string CodFteGral { get; set; }
    }
}