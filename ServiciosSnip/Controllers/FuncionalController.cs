using Dapper;
using Microsoft.AspNetCore.Mvc;
using ServiciosSnip.Services;
using System.Data;


namespace SnipAuthServerV1.Controllers
{
    [ApiController]
    [Route("/servicios/v1/sigef/cla/funcional")]
    public class FuncionalController : Controller
    {
        private readonly IDbConnection _dbConnection;
        private readonly ExternalApiService _externalApiService;

        public FuncionalController(IDbConnection dbConnection, ExternalApiService externalApiService)
        {
            _dbConnection = dbConnection;
            _externalApiService = externalApiService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFuentesFuncional([FromQuery] string estado = "vigente")
        {
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
            string url = $"https://localhost:7261/api/clasificadores/sigeft/fuente?estado={estado}";

            // Consumir el servicio externo
            var fuentesExternas = await _externalApiService.GetFuentesFinanciamientosAsync(url, token);

            if (fuentesExternas == null || fuentesExternas.datos.Count == 0)
            {
                return NotFound(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontraron registros en el servicio externo."
                });
            }

            // Retornar los datos obtenidos
            return Ok(new
            {
                estatus_code = "200",
                estatus_msg = "Registros obtenidos correctamente.",
                data = fuentesExternas.datos
            });
        }

        [HttpPost]
        public async Task<IActionResult> InsertFuenteFromExternalService([FromBody] CodSuFuncionRequest request)
        {
            // Validar que el campo codFteGral sea obligatorio
            if (string.IsNullOrEmpty(request.cod_sub_funcion))
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
            string url = $"https://localhost:7261/api/clasificadores/sigeft/fuente/{request.cod_sub_funcion}";

            // Consumir el servicio externo
            var fuenteExterna = await _externalApiService.GetFuenteExternaAsync(url, token);

            if (fuenteExterna == null)
            {
                return BadRequest(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontraron fuentes externas para insertar."
                });
            }

     

            // Insertar en la base de datos utilizando el procedimiento almacenado
            var result = _dbConnection.Execute("dbo.f_cla_funcional_ins", new
            {
                id_funcional = 0, //Indica que deseas asignar el ID automáticamente
                cod_finalidad = request.cod_finalidad,
                cod_funcion = request.cod_funcion,
                cod_sub_funcion = request.cod_sub_funcion,
                descripcion = request.descripcion_sub_funcion,
                terminal = "S",
                activo = fuenteExterna.estado == "habilitado" ? "S" : "N",
                estado = "registrar",
                bandeja = 0,
                usu_ins = 1,
                fec_ins = DateTime.Now,
                usu_upd = 1,
                fec_upd = DateTime.Now,
            }, commandType: CommandType.StoredProcedure);

            return Ok(new
            {
                estatus_code = "201",
                estatus_msg = "Fuente registrada correctamente a partir del servicio externo."
            });
        }



        // Clase para recibir el JSON del cliente
        public class CodSuFuncionRequest
        {
            public string cod_finalidad { get; set; }
            public string descripcion_finalidad { get; set; }
            public string cod_funcion { get; set; }

            public string descripcion_funcion { get; set;}

            public string cod_sub_funcion { get; set;}
            public string descripcion_sub_funcion { get; set; }
            public string estado { get; set; }

            public string condicion { get; set; }
        }
    }
}
