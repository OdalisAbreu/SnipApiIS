using Dapper;
using Microsoft.AspNetCore.Mvc;
using CatalogosSnipSigef.Models;
using CatalogosSnipSigef.Services;
using System.Data;
using Microsoft.AspNetCore.Authorization;

namespace CatalogosSnipSigef.Controllers
{
    [ApiController]
    [Route("/servicios/v1/sigef/cla/cta-presupuestaria")]
    [Authorize]
    public class CuentaPresupuestaria : Controller
    {
        private readonly IDbConnection _dbConnection;
        private readonly ExternalApiService _externalApiService;

        public CuentaPresupuestaria(IDbConnection dbConnection, ExternalApiService externalApiService)
        {
            _dbConnection = dbConnection;
            _externalApiService = externalApiService;
        }

        [HttpGet]
        public async Task<IActionResult> GetObjetosGasto([FromQuery] string estado = "vigente")
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
            string url = $"https://localhost:7261/api/clasificadores/sigeft/ObjetosGasto?estado={estado}";

            // Consumir el servicio externo
            var CuentaPresupuestaria = await _externalApiService.GetCuentasPresupuestariasAsync(url, token);

            if (CuentaPresupuestaria == null)
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
                data = CuentaPresupuestaria
            });
        }

        [HttpPost]
        public async Task<IActionResult> InsertObjetalesFromExternalService([FromBody] CodObjetalRequest request)
        {
            if (string.IsNullOrEmpty(request.cod_objetal))
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
            string url = $"https://localhost:7261/api/clasificadores/sigeft/ObjetosGasto/{request.cod_objetal}";

            // Consumir el servicio externo
            var fuenteExterna = await _externalApiService.GetCuentaPresupuestariaAsync(url, token);

            if (fuenteExterna == null)
            {
                return BadRequest(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontraron fuentes externas para insertar."
                });
            }

  

            // Insertar en la base de datos utilizando el procedimiento almacenado
            var result = _dbConnection.Execute("dbo.f_cla_objetales_ins", new
            {
                id_objetal = 0, //Indica que deseas asignar el ID automáticamente
                id_version = 1,
                cod_objetal = fuenteExterna.cod_tipo + '.' + fuenteExterna.cod_objeto + '.' + fuenteExterna.cod_cuenta + '.' + fuenteExterna.cod_sub_cuenta + '.' + fuenteExterna.cod_auxiliar,
                descripcion = fuenteExterna.descripcion_objeto,
                cod_objetal_superior = fuenteExterna.cod_tipo + '.' + fuenteExterna.cod_objeto + '.' + fuenteExterna.cod_cuenta + '.' + fuenteExterna.cod_sub_cuenta,
                cod_despliegue = fuenteExterna.cod_tipo + fuenteExterna.cod_objeto + fuenteExterna.cod_cuenta + fuenteExterna.cod_sub_cuenta + fuenteExterna.cod_auxiliar,
                activo = fuenteExterna.estado == "habilitado" ? "S" : "N",
                terminal = "S",
                inversion = "S",
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateObjetales(int id, [FromBody] UpdateobjetalesRequest request)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    estatus_code = "400",
                    estatus_msg = "El ID es obligatorio y debe ser mayor que cero."
                });
            }

            try
            {
                // Obtener los datos actuales con f_cla_fuentes_generales_leer
                var datosExistentes = _dbConnection.QueryFirstOrDefault<dynamic>("dbo.f_cla_objetales_leer", new
                {
                    id_objetal = id,
                    estado = "S",
                    usu_upd = 1
                }, commandType: CommandType.StoredProcedure);

                if (datosExistentes == null)
                {
                    return NotFound(new
                    {
                        estatus_code = "404",
                        estatus_msg = "No se encontró la fuente especificada."
                    });
                }

                // Combinar los datos proporcionados con los datos existentes
                var idVersion = request.id_version ?? datosExistentes.id_version;
                var codObjetal = !string.IsNullOrEmpty(request.cod_objetal) ? request.cod_objetal : datosExistentes.cod_objetal;
                var descripcion = !string.IsNullOrEmpty(request.descripcion) ? request.descripcion.ToUpper() : datosExistentes.descripcion;
                var codObjetalSuperior = !string.IsNullOrEmpty(request.cod_objetal_superior) ? request.cod_objetal_superior : datosExistentes.cod_objetal_superior;
                var codDespliegue = !string.IsNullOrEmpty(request.cod_despliegue) ? request.cod_despliegue : datosExistentes.cod_despliegue;
                var terminal = !string.IsNullOrEmpty(request.terminal) ? request.terminal : datosExistentes.terminal;
                var inversion = !string.IsNullOrEmpty(request.inversion) ? request.inversion : datosExistentes.inversion;
                var activo = !string.IsNullOrEmpty(request.activo) ? request.activo : datosExistentes.activo;
                var estado = "actualizar";
                var bandeja = request.bandeja ?? datosExistentes.bandeja;
                var usuIns = 1;
                var fecIns = request.fec_ins ?? datosExistentes.fec_ins;
                var usuUpd = 1;
                var fecUpd = request.fec_upd ?? datosExistentes.fec_upd;

                // Crear parámetros para el procedimiento de actualización
                var parametros = new DynamicParameters();
                parametros.Add("id_objetal", id);
                parametros.Add("id_version", idVersion);
                parametros.Add("cod_objetal", codObjetal);
                parametros.Add("descripcion", descripcion);
                parametros.Add("cod_objetal_superior", codObjetalSuperior);
                parametros.Add("cod_despliegue", codDespliegue);
                parametros.Add("terminal", terminal);
                parametros.Add("inversion", inversion);
                parametros.Add("activo", activo);
                parametros.Add("estado", estado);
                parametros.Add("bandeja", bandeja);
                parametros.Add("fec_ins", fecIns);
                parametros.Add("usu_ins", usuIns);
                parametros.Add("usu_upd", usuUpd);
                parametros.Add("fec_upd", DateTime.Now);
                // Ejecutar el procedimiento de actualización
                var returnValue = _dbConnection.QuerySingle<int>("dbo.f_cla_objetales_upd", parametros, commandType: CommandType.StoredProcedure);

                if (returnValue > 0)
                {
                    return Ok(new
                    {
                        estatus_code = "200",
                        estatus_msg = "Fuente actualizada correctamente."
                    });
                }

                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "No se pudo actualizar la fuente."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "Ocurrió un error al intentar actualizar la fuente.",
                    detalle_error = ex.Message
                });
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteObjetales(int id)
        {

            // Validar si la fuente existe
            var existe = _dbConnection.ExecuteScalar<int>("dbo.[f_cla_objetales_leer]", new
            {
                id_objetal = id,
                estado = "S",
                usu_upd = 1 // Usuario de validación
            }, commandType: CommandType.StoredProcedure);

            if (existe <= 0)
            {
                return NotFound(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontró la fuente especificada."
                });
            }

            // Ejecutar el procedimiento para eliminar fuente general
            var result = _dbConnection.Execute("dbo.f_cla_objetales_del", new
            {
                id_objetal = id,
                estado = "S",
                usu_upd = 1 // Usuario que realiza la acción
            }, commandType: CommandType.StoredProcedure);

            return Ok(new
            {
                estatus_code = "200",
                estatus_msg = "Fuente eliminada correctamente."
            });

        }

        // Clase para recibir el JSON del cliente
        public class CodObjetalRequest
        {
            public string cod_objetal { get; set; }

        }
    }
}
