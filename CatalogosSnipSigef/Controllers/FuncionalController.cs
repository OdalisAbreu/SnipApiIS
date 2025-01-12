using Dapper;
using Microsoft.AspNetCore.Mvc;
using CatalogosSnipSigef.Services;
using System.Data;
using CatalogosSnipSigef.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


namespace CatalogosSnipSigef.Controllers
{
    [ApiController]
    [Route("/servicios/v1/sigef/cla/funcional")]
    [Authorize]
    public class FuncionalController : Controller
    {
        private readonly IDbConnection _dbConnection;
        private readonly ExternalApiService _externalApiService;
        private readonly string _urlApiBase;
        private readonly ILogService _logService;
        public FuncionalController(IDbConnection dbConnection, ExternalApiService externalApiService, IConfiguration configuration, ILogService logService)
        {
            _dbConnection = dbConnection;
            _externalApiService = externalApiService;
            _urlApiBase = configuration["SigefApi:Url"];
            _logService = logService;
        }

        [HttpGet]
        public async Task<IActionResult> GetFuncional([FromQuery] int? id_funcional = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            var query = "SELECT * FROM cla_funcional WHERE activo = 'S' AND 1=1";
            var parameters = new DynamicParameters();

            if (id_funcional.HasValue) 
            {
                query += "AND id_funcional = @id_funcional";
                parameters.Add("id_funcional", id_funcional.Value);
            }
            var funcionales = await _dbConnection.QueryAsync(query, parameters);
            var totalRegistros = funcionales.Count();

            if (totalRegistros < 1)
            {
                return BadRequest(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontraron funcionales."
                });
            }
            var objet = new List<object>();
            objet.Add(new
            {
                total_registros = totalRegistros,
                cla_funcionales = funcionales
            });
            await _logService.LogAsync("Info", $"Usuario: {userName} Consulta Funcional", int.Parse(userId));
            return Ok(objet[0]);
        }

        [HttpPost]
        public async Task<IActionResult> InsertFuncionalFromExternalService([FromBody] CodFuncionRequest? request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";
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

            if (request == null || string.IsNullOrEmpty(request.cod_su_funcion))
            {
                var responseJson = new List<object>(); // Lista para acumular los resultados de las iteraciones
                string urlFull = $"https://localhost:7261/api/clasificadores/sigeft/fuente/{request.cod_su_funcion}";

                // Consumir el servicio externo
                var fuentesExternasResponse = await _externalApiService.GetFuentesExternasAsync(urlFull, token);

                if (fuentesExternasResponse != null && fuentesExternasResponse.datos != null)
                {
                    foreach (var fuente in fuentesExternasResponse.datos)
                    {
                        try
                        {
                            var resultJson = _dbConnection.Execute("dbo.f_cla_funcional_ins", new
                            {
                                id_funcional = 0, //Indica que deseas asignar el ID automáticamente
                                cod_finalidad = fuente.cod_finalidad,
                                cod_funcion = fuente.cod_funcion,
                                cod_sub_funcion = fuente.cod_sub_funcion,
                                descripcion = fuente.descripcion_sub_funcion,
                                terminal = "S",
                                activo = fuente.estado == "habilitado" ? "S" : "N",
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
                                cod_su_funcion = $"{fuente.cod_finalidad}{fuente.cod_funcion}{fuente.cod_sub_funcion}",
                                descripcion = fuente.descripcion_sub_funcion
                            });
                        }
                        catch (Exception ex)
                        {
                            // Construir la entrada de error
                            responseJson.Add(new
                            {
                                status = "fail",
                                cod_su_funcion = $"{fuente.cod_finalidad}{fuente.cod_funcion}{fuente.cod_sub_funcion}",
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
                        estatus_msg = "No se encontraron fuentes externas para insertar."
                    });
                }
                await _logService.LogAsync("Info", $"Usuario: {userName} Registra Funcionales", int.Parse(userId));
                return Ok(new
                {
                    estatus_code = "201",
                    estatus_msg = "Fuentes registradas correctamente a partir del servicio externo.",
                    register_status = responseJson
                });
            }


            // Construir la URL con los parámetros requeridos
            string url = $"https://localhost:7261/api/clasificadores/sigeft/fuente/{request.cod_su_funcion}";


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
                cod_finalidad = fuenteExterna.cod_finalidad,
                cod_funcion = fuenteExterna.cod_funcion,
                cod_sub_funcion = fuenteExterna.cod_sub_funcion,
                descripcion = fuenteExterna.descripcion_sub_funcion,
                terminal = "S",
                activo = fuenteExterna.estado == "habilitado" ? "S" : "N",
                estado = "registrar",
                bandeja = 0,
                usu_ins = userId,
                fec_ins = DateTime.Now,
                usu_upd = userId,
                fec_upd = DateTime.Now,
            }, commandType: CommandType.StoredProcedure);
            await _logService.LogAsync("Info", $"Usuario: {userName} Registra Funcional a las {DateTime.Now}", int.Parse(userId));
            return Ok(new
            {
                estatus_code = "201",
                estatus_msg = "Fuente registrada correctamente a partir del servicio externo."
            });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFuncional(int id, [FromBody] UpdateCodFuncionRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

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
                var datosExistentes = _dbConnection.QueryFirstOrDefault<dynamic>("dbo.f_cla_funcional_leer", new
                {
                    id_funcional = id,
                    estado = "S",
                    usu_upd = userId
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
                var cod_finalidad = request.cod_finalidad ?? datosExistentes.cod_finalidad;
                var cod_funcion = !string.IsNullOrEmpty(request.cod_funcion) ? request.cod_funcion : datosExistentes.cod_funcion;
                var cod_sub_funcion = !string.IsNullOrEmpty(request.cod_sub_funcion) ? request.cod_sub_funcion : datosExistentes.cod_sub_funcion;
                var descripcion = !string.IsNullOrEmpty(request.descripcion) ? request.descripcion.ToUpper() : datosExistentes.descripcion;
                var terminal = !string.IsNullOrEmpty(request.terminal) ? request.terminal : datosExistentes.terminal;
                var activo = !string.IsNullOrEmpty(request.activo) ? request.activo : datosExistentes.activo;
                var estado = "actualizar";
                var bandeja = request.bandeja ?? datosExistentes.bandeja;
                var usuIns = request.usu_ins ?? datosExistentes.usu_ins;
                var fecIns = request.fec_ins ?? datosExistentes.fec_ins;

                // Crear parámetros para el procedimiento de actualización
                var parametros = new DynamicParameters();
                parametros.Add("id_funcional", id);
                parametros.Add("cod_finalidad", cod_finalidad);
                parametros.Add("cod_funcion", cod_funcion);
                parametros.Add("cod_sub_funcion", cod_sub_funcion);
                parametros.Add("descripcion", descripcion);
                parametros.Add("terminal", terminal);
                parametros.Add("activo", activo);
                parametros.Add("estado", estado);
                parametros.Add("bandeja", bandeja);
                parametros.Add("usu_ins", usuIns);
                parametros.Add("fec_ins", fecIns);
                parametros.Add("usu_upd", userId);
                parametros.Add("fec_upd", DateTime.Now);

                // Ejecutar el procedimiento de actualización
                var returnValue = _dbConnection.QuerySingle<int>("dbo.f_cla_funcional_upd", parametros, commandType: CommandType.StoredProcedure);

                if (returnValue > 0)
                {
                    await _logService.LogAsync("Info", $"Usuario: {userName} Actualizar Funcional id: {id}", int.Parse(userId));
                    return Ok(new
                    {
                        estatus_code = "200",
                        estatus_msg = "Fuente actualizada correctamente."
                    });
                }

                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "No se pudo actualizar funcional."
                });
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("Error", ex.Message + $" Usuario: {userName} actualiza funcional id: {id}", int.Parse(userId));
                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "Ocurrió un error al intentar actualizar funcional.",
                    detalle_error = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFuncional(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            // Validar si la fuente existe
            var existe = _dbConnection.ExecuteScalar<int>("dbo.[f_cla_funcional_leer]", new
            {
                id_funcional = id,
                estado = "S",
                usu_upd = userId
            }, commandType: CommandType.StoredProcedure);

            if (existe <= 0)
            {
                return NotFound(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontró el funcional."
                });
            }
            try
            {
                var result = _dbConnection.Execute("dbo.[f_cla_funcional_del]", new
                {
                    id_funcional = id,
                    estado = "S",
                    usu_upd = userId
                }, commandType: CommandType.StoredProcedure);
                await _logService.LogAsync("Info", $"Usuario: {userName} Elimina funcional id: {id}", int.Parse(userId));
                return Ok(new
                {
                    estatus_code = "200",
                    estatus_msg = "Funcional eliminada correctamente."
                });
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("Error", ex.Message + $" Usuario: {userName} Eliminar funcional id: {id}", int.Parse(userId));
                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "Ocurrió un error al intentar eliminar funcional.",
                    detalle_error = ex.Message
                });
            }
        }

     }
}
