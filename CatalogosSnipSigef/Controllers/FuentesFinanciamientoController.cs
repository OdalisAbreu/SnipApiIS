using Dapper;
using Microsoft.AspNetCore.Mvc;
using CatalogosSnipSigef.Models;
using CatalogosSnipSigef.Services;
using System.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using Azure.Core;
using Newtonsoft.Json;


namespace CatalogosSnipSigef.Controllers
{
    [ApiController]
    [Route("/servicios/v1/sigef/cla/financiamiento/fgeneral")]
    [Authorize]
    public class FuentesFinanciamientoController : Controller
    {
            private readonly IDbConnection _dbConnection;
            private readonly ExternalApiService _externalApiService;
            private readonly IConfiguration _configuration;
            private readonly string _urlApiBase;
            private readonly ILogService _logService;
            private readonly string _ip;
            private readonly string _route;

        public FuentesFinanciamientoController(IDbConnection dbConnection, ExternalApiService externalApiService, IConfiguration configuration, ILogService logService)
            {
                _dbConnection = dbConnection;
                _externalApiService = externalApiService;
                _configuration = configuration;
                _urlApiBase = configuration["SigefApi:Url"];
                _logService = logService;
                _ip = "127.0.0.1";
                _route = "/servicios/v1/sigef/cla/financiamiento/fgeneral";
        }

        [HttpGet]
        public async Task<IActionResult> getFuentesGenerales([FromQuery] int? id_fte_gral)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            var query = "SELECT * FROM cla_fuentes_generales WHERE activo = 'S' AND 1=1 ";
            var parameters = new DynamicParameters();

            if (id_fte_gral.HasValue)
            {
                query += "AND id_fte_gral = @id_fte_gral";
                parameters.Add("id_fte_gral", id_fte_gral.Value);
            }
            var fuente_generales = await _dbConnection.QueryAsync(query, parameters);
            var totalRegistros = fuente_generales.Count();

            if (totalRegistros < 1) 
            {
                return BadRequest(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontraron Fuentes Generales."
                });
            }
            var objet = new List<object>();
            await _logService.LogAsync("Info", $"Usuario: {userName} Consulta fuente generales", int.Parse(userId), _ip, _route, $"id_fte_gral: {id_fte_gral}", $"total_registros: {totalRegistros},cla_fuente_generales: {fuente_generales} ", "GET");
            objet.Add(new
            {
                total_registros = totalRegistros,
                cla_fuente_generales = fuente_generales
            });
            return Ok(objet[0]);
        }

        [HttpGet]
        [Route("/servicios/v1/sigef/cla/financiamiento/fespecifica")]
        public async Task<IActionResult> getFuentesEspecifica([FromQuery] int? id_fte_esp)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            var query = "SELECT * FROM cla_fuentes_especificas WHERE activo = 'S' AND 1=1 ";
            var parameters = new DynamicParameters();

            if (id_fte_esp.HasValue)
            {
                query += "AND id_fte_esp = @id_fte_esp";
                parameters.Add("id_fte_esp", id_fte_esp.Value);
            }
            var fuente_especificas = await _dbConnection.QueryAsync(query, parameters);
            var totalRegistros = fuente_especificas.Count();

            if (totalRegistros < 1)
            {
                return BadRequest(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontraron Fuentes Especificas."
                });
            }
            var objet = new List<object>();
            objet.Add(new
            {
                total_registros = totalRegistros,
                cla_fuente_especificas = fuente_especificas
            });
            await _logService.LogAsync("Info", $"Usuario: {userName} Consulta fuente especifica", int.Parse(userId), _ip, "/servicios/v1/sigef/cla/financiamiento/fespecifica",$"id_fte_esp: {id_fte_esp}", JsonConvert.SerializeObject(objet[0]), "GET");
            return Ok(objet[0]);
        }

        [HttpPost]
            public async Task<IActionResult> InsertFuenteFromExternalService([FromBody] CodFteGralRequest? request)
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

            if (request == null || string.IsNullOrEmpty(request.cod_fte_gral))
            {
                var responseJson = new List<object>(); // Lista para acumular los resultados de las iteraciones
                string urlFull = $"https://localhost:6100/api/clasificadores/sigeft/FuentesDeFinanciamiento";

                // Consumir el servicio externo
                var fuenteFinanciamientoResponse = await _externalApiService.GetFuentesFinamciamientoAsync(urlFull, token);

                if (fuenteFinanciamientoResponse != null && fuenteFinanciamientoResponse.datos != null)
                {
                    foreach (var fuente in fuenteFinanciamientoResponse.datos)
                    {
                        try
                        {
                            // Validar si la fuente general ya existe
                            var existingFuenteGeneral = _dbConnection.QueryFirstOrDefault("SELECT * FROM cla_fuentes_generales WHERE cod_fte_gral = @cod_fte_gral",
                                new { cod_fte_gral = fuente.cod_fuente });

                            int idFuenteGeneral;

                            if (existingFuenteGeneral != null)
                            {
                                // Actualizar la fuente general existente
                                var parametros = new DynamicParameters();
                                parametros.Add("id_fte_gral", existingFuenteGeneral.id_fte_gral);
                                parametros.Add("id_version", 1);
                                parametros.Add("cod_fte_gral", fuente.cod_fuente);
                                parametros.Add("descripcion", fuente.descripcion_fuente.ToUpper());
                                parametros.Add("tipo_fuente", fuente.descripcion_grupo == "1" ? "I" : "E");
                                parametros.Add("activo", fuente.estado == "habilitado" ? "S" : "N");
                                parametros.Add("estado", "actualizar");
                                parametros.Add("bandeja", 0);
                                parametros.Add("usu_ins", existingFuenteGeneral.usu_ins); // Mantener el usuario original
                                parametros.Add("fec_ins", existingFuenteGeneral.fec_ins); // Mantener la fecha original
                                parametros.Add("usu_upd", userId);
                                parametros.Add("fec_upd", DateTime.Now);

                                idFuenteGeneral = _dbConnection.QuerySingle<int>("dbo.f_cla_fuentes_generales_upd", parametros, commandType: CommandType.StoredProcedure);

                                // Respuesta de actualización
                                responseJson.Add(new
                                {
                                    status = "update fuente general",
                                    cod_fte_gral = fuente.cod_fuente,
                                    descripcion = fuente.descripcion_fuente.ToUpper()
                                });
                            }
                            else
                            {
                                // Insertar una nueva fuente general
                                idFuenteGeneral = _dbConnection.QuerySingle<int>("dbo.f_cla_fuentes_generales_ins", new
                                {
                                    id_fte_gral = 0,
                                    id_version = 1,
                                    cod_fte_gral = fuente.cod_fuente,
                                    descripcion = fuente.descripcion_fuente.ToUpper(),
                                    tipo_fuente = fuente.descripcion_grupo == "1" ? "I" : "E",
                                    activo = fuente.estado == "habilitado" ? "S" : "N",
                                    estado = "registrar",
                                    bandeja = 0,
                                    usu_ins = userId,
                                    fec_ins = DateTime.Now,
                                    usu_upd = userId,
                                    fec_upd = DateTime.Now,
                                }, commandType: CommandType.StoredProcedure);

                                // Respuesta de creación
                                responseJson.Add(new
                                {
                                    status = "create fuente general",
                                    cod_fte_gral = fuente.cod_fuente,
                                    descripcion = fuente.descripcion_fuente.ToUpper()
                                });
                            }

                            // Validar si la fuente específica ya existe
                            var existingFuenteEspecifica = _dbConnection.QueryFirstOrDefault("SELECT * FROM cla_fuentes_especificas WHERE id_fte_gral = @id_fte_gral AND cod_fte_esp = @cod_fte_esp",
                                new
                                {
                                    id_fte_gral = idFuenteGeneral,
                                    cod_fte_esp = fuente.cod_fuente_especifica
                                });

                            if (existingFuenteEspecifica != null)
                            {
                                // Actualizar la fuente específica existente
                                var parametrosEsp = new DynamicParameters();
                                parametrosEsp.Add("id_fte_esp", existingFuenteEspecifica.id_fte_esp);
                                parametrosEsp.Add("id_version", 1);
                                parametrosEsp.Add("id_fte_gral", idFuenteGeneral);
                                parametrosEsp.Add("cod_fte_esp", fuente.cod_fuente_especifica);
                                parametrosEsp.Add("descripcion", fuente.descripcion_fuente_especifica.ToUpper());
                                parametrosEsp.Add("activo", fuente.estado == "habilitado" ? "S" : "N");
                                parametrosEsp.Add("estado", "actualizar");
                                parametrosEsp.Add("bandeja", 0);
                                parametrosEsp.Add("usu_ins", existingFuenteEspecifica.usu_ins); // Mantener el usuario original
                                parametrosEsp.Add("fec_ins", existingFuenteEspecifica.fec_ins); // Mantener la fecha original
                                parametrosEsp.Add("usu_upd", userId);
                                parametrosEsp.Add("fec_upd", DateTime.Now);

                                _dbConnection.QuerySingle<int>("dbo.f_cla_fuentes_especificas_upd", parametrosEsp, commandType: CommandType.StoredProcedure);

                                // Respuesta de actualización
                                responseJson.Add(new
                                {
                                    status = "update fuente especifica",
                                    cod_fte_gral = $"{fuente.cod_fuente}.",
                                    cod_fte_espe = $"{fuente.cod_fuente_especifica}",
                                    descripcion = fuente.descripcion_fuente_especifica.ToUpper()
                                });
                            }
                            else
                            {
                                // Insertar una nueva fuente específica
                                _dbConnection.Execute("dbo.f_cla_fuentes_especificas_ins", new
                                {
                                    id_fte_esp = 0,
                                    id_version = 1,
                                    id_fte_gral = idFuenteGeneral,
                                    cod_fte_esp = fuente.cod_fuente_especifica,
                                    descripcion = fuente.descripcion_fuente_especifica.ToUpper(),
                                    activo = fuente.estado == "habilitado" ? "S" : "N",
                                    estado = "registrar",
                                    bandeja = 0,
                                    usu_ins = userId,
                                    fec_ins = DateTime.Now,
                                    usu_upd = userId,
                                    fec_upd = DateTime.Now,
                                }, commandType: CommandType.StoredProcedure);

                                // Respuesta de creación
                                responseJson.Add(new
                                {
                                    status = "create fuente especifica",
                                    cod_fte_gral = $"{fuente.cod_fuente}.",
                                    cod_fte_espe = $"{fuente.cod_fuente_especifica}",
                                    descripcion = fuente.descripcion_fuente_especifica.ToUpper()
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Manejar errores y construir la respuesta de fallo
                            responseJson.Add(new
                            {
                                status = "fail",
                                cod_fte_gral = $"{fuente.cod_fuente}.{fuente.cod_fuente_especifica}",
                                details = ex.Message
                            });
                        }
                    }

                    await _logService.LogAsync("Info", $"Usuario: {userName} Insertar fuentes generales y específicas", int.Parse(userId), _ip, _route, $"cod_fte_gral: NULL", $"estatus_code: 201, register_status: {responseJson} ", "POST");
                    return Ok(new
                    {
                        estatus_code = "201",
                        estatus_msg = "Fuentes generales y específicas procesadas correctamente.",
                        register_status = responseJson
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        estatus_code = "404",
                        estatus_msg = "No se encontraron fuentes externas para procesar."
                    });
                }
            }


            // Construir la URL con los parámetros requeridos
            string url = $"https://localhost:6100/api/clasificadores/sigeft/FuentesDeFinanciamiento/{request.cod_fte_gral}";

                // Consumir el servicio externo
                var fuenteExterna = await _externalApiService.GetFuenteFinamciamientoAsync(url, token);
                if (fuenteExterna == null )
                {
                    return BadRequest(new
                    {
                        estatus_code = "404",
                        estatus_msg = "No se encontraron fuentes externas para insertar."
                    });
                }


                  
                var resultGen = _dbConnection.QuerySingle<int>("dbo.f_cla_fuentes_generales_ins", new
                {
                    id_fte_gral = 0, //Indica que deseas asignar el ID automáticamente
                    id_version = 1,
                    cod_fte_gral = fuenteExterna.cod_fuente,
                    descripcion = fuenteExterna.descripcion_fuente.ToUpper(),
                    tipo_fuente = fuenteExterna.descripcion_grupo == "1" ? "I" : "E",
                    activo = fuenteExterna.estado == "habilitado" ? "S" : "N",
                    estado = "registrar",
                    bandeja = 0,
                    usu_ins = userId,
                    fec_ins = DateTime.Now,
                    usu_upd = userId,
                    fec_upd = DateTime.Now,
                }, commandType: CommandType.StoredProcedure);

                
                var resultEsp = _dbConnection.Execute("dbo.f_cla_fuentes_especificas_ins", new
                {
                    id_fte_esp = 0, //Indica que deseas asignar el ID automáticamente
                    id_version = 1,
                    id_fte_gral = resultGen,
                    cod_fte_esp = fuenteExterna.cod_fuente_especifica,
                    descripcion = fuenteExterna.descripcion_fuente_especifica.ToUpper(),
                    activo = fuenteExterna.estado == "habilitado" ? "S" : "N",
                    estado = "registrar",
                    bandeja = 0,
                    usu_ins = userId,
                    fec_ins = DateTime.Now,
                    usu_upd = userId,
                    fec_upd = DateTime.Now,
                }, commandType: CommandType.StoredProcedure);

            await _logService.LogAsync("Info", $"Usuario: {userName} Insertar fuente general y especifica", int.Parse(userId), _ip, _route, $"cod_fte_gral: {request.cod_fte_gral}", $"estatus_code: 201, estatus_msg: Fuente registrada correctamente a partir del servicio externo.", "POST");

            return Ok(new
                {
                    estatus_code = "201",
                    estatus_msg = "Fuente registrada correctamente a partir del servicio externo."
                });
            }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFuenteGeneral(int id, [FromBody] UpdateFuenteRequest request)
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
                // Obtener los datos actuales con f_cla_fuentes_generales_leer
                var datosExistentes = _dbConnection.QueryFirstOrDefault<dynamic>("dbo.f_cla_fuentes_generales_leer", new
                {
                    id_fte_gral = id,
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
                var idVersion = request.id_version ?? datosExistentes.id_version;
                var codFteGral = !string.IsNullOrEmpty(request.cod_fte_gral) ? request.cod_fte_gral : datosExistentes.cod_fte_gral;
                var descripcion = !string.IsNullOrEmpty(request.descripcion) ? request.descripcion.ToUpper() : datosExistentes.descripcion;
                var tipoFuente = !string.IsNullOrEmpty(request.tipo_fuente) ? request.tipo_fuente : datosExistentes.tipo_fuente;
                var activo = !string.IsNullOrEmpty(request.activo) ? request.activo : datosExistentes.activo;
                var estado = "actualizar";
                var bandeja = request.bandeja ?? datosExistentes.bandeja;
                var usuIns = request.usu_ins ?? datosExistentes.usu_ins;
                var fecIns = request.fec_ins ?? datosExistentes.fec_ins;

                // Crear parámetros para el procedimiento de actualización
                var parametros = new DynamicParameters();
                parametros.Add("id_fte_gral", id);
                parametros.Add("id_version", idVersion);
                parametros.Add("cod_fte_gral", codFteGral);
                parametros.Add("descripcion", descripcion);
                parametros.Add("tipo_fuente", tipoFuente);
                parametros.Add("activo", activo);
                parametros.Add("estado", estado);
                parametros.Add("bandeja", bandeja);
                parametros.Add("usu_ins", usuIns);
                parametros.Add("fec_ins", fecIns);
                parametros.Add("usu_upd", userId);
                parametros.Add("fec_upd", DateTime.Now);

                // Ejecutar el procedimiento de actualización
                var returnValue = _dbConnection.QuerySingle<int>("dbo.f_cla_fuentes_generales_upd", parametros, commandType: CommandType.StoredProcedure);

                if (returnValue > 0)
                {
                    await _logService.LogAsync("Info", $"Usuario: {userName} Actualiza fuente general id: {id}", int.Parse(userId), _ip, _route, JsonConvert.SerializeObject(request), $"estatus_code: 200", "PUT");
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
                await _logService.LogAsync("Error", ex.Message + $" Usuario: {userName} Actualiza objetal id: {id}", int.Parse(userId), _ip, _route, JsonConvert.SerializeObject(request), $"estatus_code: 500", "PUT");
                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "Ocurrió un error al intentar actualizar la fuente.",
                    detalle_error = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFuente(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            // Validar si la fuente existe
            var result = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>("dbo.[f_cla_fuentes_generales_leer]", new
            {
                id_fte_gral = id,
                estado = "S",
                usu_upd = userId
            }, commandType: CommandType.StoredProcedure);

            if (result == null)
            {
                return NotFound(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontró la fuente especificada."
                });
            }
            var query = "SELECT * FROM cla_fuentes_especificas WHERE 1=1";
            var parameters = new DynamicParameters();

            query += "AND id_fte_gral = @id_fte_gral";
            parameters.Add("id_fte_gral", id);

            var fEspecifica = await _dbConnection.QueryAsync(query, parameters);

            //return fEspecifica;
            try
            {
            foreach(var especifica in fEspecifica)
            {
                // Ejecutar el procedimiento para eliminar fuente especifica
                var Especifica = _dbConnection.Execute("dbo.[f_cla_fuentes_especificas_del]", new
                {
                    id_fte_esp = especifica.id_fte_esp,
                    estado = "S",
                    usu_upd = userId
                }, commandType: CommandType.StoredProcedure);
            }
                // Ejecutar el procedimiento para eliminar fuente general
                var procedure = _dbConnection.Execute("dbo.[f_cla_fuentes_generales_del]", new
                {
                    id_fte_gral = id,
                    estado = "S",
                    usu_upd = userId
                }, commandType: CommandType.StoredProcedure);
                await _logService.LogAsync("Info", $"Usuario: {userName} Elimina fuentes fuente_general_id: {id}", int.Parse(userId), _ip, _route, $"id: {id}", " estatus_code = 200", "DELETE");
                return Ok(new
                {
                    estatus_code = "200",
                    estatus_msg = "Fuente eliminada correctamente."
                });
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("Error", ex.Message + $" Usuario: {userName} Eliminar objetal id: {id}", int.Parse(userId), _ip, _route, $"id: {id}", " estatus_code = 200", "DELETE");
                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "Ocurrió un error al intentar actualizar la fuente.",
                    detalle_error = ex.Message
                });
            }


        }
        // Clase para recibir el JSON del cliente
        public class CodFteGralRequest
            {
                public string cod_fte_gral { get; set; }
            }
        }
    
}
