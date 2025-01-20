using Dapper;
using Microsoft.AspNetCore.Mvc;
using CatalogosSnipSigef.Models;
using CatalogosSnipSigef.Services;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Net;
using Newtonsoft.Json;
using Azure.Core;

namespace CatalogosSnipSigef.Controllers
{
    [ApiController]
    [Route("/servicios/v1/sigef/cla/cta-presupuestaria")]
    [Authorize]
    public class CuentaPresupuestaria : Controller
    {
        private readonly IDbConnection _dbConnection;
        private readonly ExternalApiService _externalApiService;
        private readonly string _urlApiBase;
        private readonly ILogService _logService;
        private readonly string _ip;
        private readonly string _route;

        public CuentaPresupuestaria(IDbConnection dbConnection, ExternalApiService externalApiService, IConfiguration configuration, ILogService logService)
        {
            _dbConnection = dbConnection;
            _externalApiService = externalApiService;
            _urlApiBase = configuration["SigefApi:Url"];
            _logService = logService;
            _ip = "127.0.0.1";
            _route = "/servicios/v1/sigef/cla/cta-presupuestaria";
        }

        [HttpGet]
        public async Task<IActionResult> getObjetales([FromQuery] int? id_objetal = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            var query = "SELECT * FROM cla_objetales WHERE activo = 'S' AND 1=1";
            var parameters = new DynamicParameters();

            if (id_objetal.HasValue)
            {
                query += "AND id_objetal = @id_objetal";
                parameters.Add("id_objetal", id_objetal.Value);
            }

            var objetales = await _dbConnection.QueryAsync(query, parameters);
            var totalRegistros = objetales.Count();

            if (totalRegistros < 1)
            {
                return BadRequest(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontraron objetales."
                });
            }
            var objet = new List<object>();
            objet.Add(new
            {
                total_registros = totalRegistros,
                cla_objetales = objetales
            });
            await _logService.LogAsync("Info", $"Usuario: {userName} Consulta objetales", int.Parse(userId), _ip, _route, $"id_objetal: {id_objetal}", JsonConvert.SerializeObject(objet[0]), "GET");
            return Ok(objet[0]);
        }

        [HttpPost]
        public async Task<IActionResult> InsertObjetalesFromExternalService([FromBody] CodObjetalRequest? request)
        {
            // Autenticación: Obtener el token de acceso
            var token = await _externalApiService.GetAuthTokenAsync();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            if (string.IsNullOrEmpty(token))
            {
                return StatusCode(401, new
                {
                    estatus_code = "401",
                    estatus_msg = "No se pudo autenticar con el servicio externo."
                });
            }


            if (request == null || string.IsNullOrEmpty(request.cod_objetal))
            {
                var responseJson = new List<object>(); // Lista para acumular los resultados de las iteraciones
                string urlFull = $"https://localhost:6100/api/clasificadores/sigeft/ObjetosGasto";

                var cuentaPresupestariaResponse = await _externalApiService.GetCuentasPresupuestariasAsync(urlFull, token);

                if (cuentaPresupestariaResponse != null && cuentaPresupestariaResponse.datos != null)
                {
                    foreach (var cuenta in cuentaPresupestariaResponse.datos)
                    {
                        try
                        {
                            // Generar código objetal
                            var codObjetal = $"{cuenta.cod_tipo}.{cuenta.cod_objeto}.{cuenta.cod_cuenta}.{cuenta.cod_sub_cuenta}.{cuenta.cod_auxiliar}";

                            // Validar si ya existe en la base de datos
                            var existingObjetal = _dbConnection.QueryFirstOrDefault("SELECT * FROM cla_objetales WHERE cod_objetal = @cod_objetal",
                                new { cod_objetal = codObjetal });

                            if (existingObjetal != null)
                            {
                                // Actualizar el registro existente
                                var parametros = new DynamicParameters();
                                parametros.Add("id_objetal", existingObjetal.id_objetal);
                                parametros.Add("id_version", 1);
                                parametros.Add("cod_objetal", codObjetal);
                                parametros.Add("descripcion", cuenta.descripcion_objeto);
                                parametros.Add("cod_objetal_superior", $"{cuenta.cod_tipo}.{cuenta.cod_objeto}.{cuenta.cod_cuenta}.{cuenta.cod_sub_cuenta}");
                                parametros.Add("cod_despliegue", $"{cuenta.cod_tipo}{cuenta.cod_objeto}{cuenta.cod_cuenta}{cuenta.cod_sub_cuenta}{cuenta.cod_auxiliar}");
                                parametros.Add("terminal", "S");
                                parametros.Add("inversion", "S");
                                parametros.Add("activo", cuenta.estado == "habilitado" ? "S" : "N");
                                parametros.Add("estado", "actualizar");
                                parametros.Add("bandeja", 0);
                                parametros.Add("fec_ins", existingObjetal.fec_ins); // Mantener la fecha de inserción original
                                parametros.Add("usu_ins", existingObjetal.usu_ins); // Mantener el usuario de inserción original
                                parametros.Add("usu_upd", userId);
                                parametros.Add("fec_upd", DateTime.Now);

                                var returnValue = _dbConnection.QuerySingle<int>("dbo.f_cla_objetales_upd", parametros, commandType: CommandType.StoredProcedure);

                                // Construir la entrada de actualización
                                responseJson.Add(new
                                {
                                    status = "update",
                                    cod_objetal = codObjetal,
                                    descripcion = cuenta.descripcion_objeto
                                });
                            }
                            else
                            {
                                // Insertar un nuevo registro si no existe
                                var resultJson = _dbConnection.Execute("dbo.f_cla_objetales_ins", new
                                {
                                    id_objetal = 0, // Indica que deseas asignar el ID automáticamente
                                    id_version = 1,
                                    cod_objetal = codObjetal,
                                    descripcion = cuenta.descripcion_objeto,
                                    cod_objetal_superior = $"{cuenta.cod_tipo}.{cuenta.cod_objeto}.{cuenta.cod_cuenta}.{cuenta.cod_sub_cuenta}",
                                    cod_despliegue = $"{cuenta.cod_tipo}{cuenta.cod_objeto}{cuenta.cod_cuenta}{cuenta.cod_sub_cuenta}{cuenta.cod_auxiliar}",
                                    activo = cuenta.estado == "habilitado" ? "S" : "N",
                                    terminal = "S",
                                    inversion = "S",
                                    estado = "registrar",
                                    bandeja = 0,
                                    usu_ins = userId,
                                    fec_ins = DateTime.Now,
                                    usu_upd = userId,
                                    fec_upd = DateTime.Now,
                                }, commandType: CommandType.StoredProcedure);

                                // Construir la entrada de éxito
                                responseJson.Add(new
                                {
                                    status = "create",
                                    cod_objetal = codObjetal,
                                    descripcion = cuenta.descripcion_objeto
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Construir la entrada de error
                            responseJson.Add(new
                            {
                                status = "fail",
                                cod_objetal = $"{cuenta.cod_tipo}.{cuenta.cod_objeto}.{cuenta.cod_cuenta}.{cuenta.cod_sub_cuenta}.{cuenta.cod_auxiliar}",
                                details = ex.Message
                            });

                        }
                    }
                }
                else
                {
                    return BadRequest(new
                    {
                        estatus_code = "404",
                        estatus_msg = "No se encontraron objetales externos para insertar."
                    });
                }

                await _logService.LogAsync("Info", $"Usuario: {userName} procesa objetales masivos", int.Parse(userId), _ip, _route, $"cod_objetal: null", $" estatus_code = 201, estatus_msg = Proceso completado con éxito., register_status = {responseJson}", "POST");
                return Ok(new
                {
                    estatus_code = "201",
                    estatus_msg = "Proceso completado con éxito.",
                    register_status = responseJson
                });
            }

            // Construir la URL con los parámetros requeridos
            string url = $"https://localhost:6100/api/clasificadores/sigeft/ObjetosGasto/{request.cod_objetal}";

            // Consumir el servicio externo
            var fuenteExterna = await _externalApiService.GetCuentaPresupuestariaAsync(url, token);

            if (fuenteExterna == null)
            {
                return BadRequest(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontraron objetales externos para insertar."
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
                usu_ins = userId,
                fec_ins = DateTime.Now,
                usu_upd = userId,
                fec_upd = DateTime.Now,
            }, commandType: CommandType.StoredProcedure);

            await _logService.LogAsync("Info", $"Usuario: {userName} Registra objetales", int.Parse(userId), _ip, _route, $"cod_objetal: {request.cod_objetal}", "estatus_code = 201", "POST");

            return Ok(new
            {
                estatus_code = "201",
                estatus_msg = "Objetal registrado correctamente a partir del servicio externo."
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

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            try
            {
                // Obtener los datos actuales con f_cla_fuentes_generales_leer
                var datosExistentes = _dbConnection.QueryFirstOrDefault<dynamic>("dbo.f_cla_objetales_leer", new
                {
                    id_objetal = id,
                    estado = "S",
                    usu_upd = userId
                }, commandType: CommandType.StoredProcedure);

                if (datosExistentes == null)
                {
                    return NotFound(new
                    {
                        estatus_code = "404",
                        estatus_msg = "No se encontró el objetal especificada."
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
                var usuIns = userId;
                var fecIns = request.fec_ins ?? datosExistentes.fec_ins;
                var usuUpd = userId;
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

                await _logService.LogAsync("Info", $"Usuario: {userName} actualiza objetales id: {id}", int.Parse(userId), _ip, _route, JsonConvert.SerializeObject(request), " estatus_code = 200", "PUT");

                if (returnValue > 0)
                {
                    return Ok(new
                    {
                        estatus_code = "200",
                        estatus_msg = "Objetal actualizada correctamente."
                    });
                }

                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "No se pudo actualizar el Objetal."
                });
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("Error", ex.Message + $" Usuario: {userName} actualiza objetal id: {id}", int.Parse(userId), _ip, _route, JsonConvert.SerializeObject(request), " estatus_code = 200", "PUT");
                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "Ocurrió un error al intentar actualizar el Objetal.",
                    detalle_error = ex.Message
                });
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteObjetales(int id)
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            // Validar si la fuente existe
            var existe = _dbConnection.ExecuteScalar<int>("dbo.[f_cla_objetales_leer]", new
            {
                id_objetal = id,
                estado = "S",
                usu_upd = userId
            }, commandType: CommandType.StoredProcedure);

            if (existe <= 0)
            {
                return NotFound(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontró el Objetal especificado."
                });
            }

            // Ejecutar el procedimiento para eliminar fuente general
            var result = _dbConnection.Execute("dbo.f_cla_objetales_del", new
            {
                id_objetal = id,
                estado = "S",
                usu_upd = userId
            }, commandType: CommandType.StoredProcedure);

            await _logService.LogAsync("Info", $"Usuario: {userName} Elimina objetal id: {id}", int.Parse(userId), _ip, _route, $"id: {id}", " estatus_code = 200", "DELETE");

            return Ok(new
            {
                estatus_code = "200",
                estatus_msg = "Objetal eliminado correctamente."
            });

        }

        // Clase para recibir el JSON del cliente
        public class CodObjetalRequest
        {
            public string cod_objetal { get; set; }

        }
    }
}
