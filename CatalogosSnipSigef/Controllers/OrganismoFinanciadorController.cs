using Dapper;
using Microsoft.AspNetCore.Mvc;
using CatalogosSnipSigef.Services;
using System.Data;
using CatalogosSnipSigef.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using Newtonsoft.Json;

namespace CatalogosSnipSigef.Controllers
{
    [ApiController]
    [Route("/servicios/v1/sigef/cla/org-financiador")]
    [Authorize]
    public class OrganismoFinanciador : Controller
    {
        private readonly IDbConnection _dbConnection;
        private readonly ExternalApiService _externalApiService;
        private readonly string _urlApiBase;
        private readonly ILogService _logService;
        private readonly string _ip;
        private readonly string _route;

        public OrganismoFinanciador(IDbConnection dbConnection, ExternalApiService externalApiService, IConfiguration configuration, ILogService logService)
        {
            _dbConnection = dbConnection;
            _externalApiService = externalApiService;
            _urlApiBase = configuration["SigefApi:Url"];
            _logService = logService;
            _ip = "127.0.0.1";
            _route = "/servicios/v1/sigef/cla/org-financiador";

        }

        [HttpGet]
        public async Task<IActionResult> GetOrganismoFinanciadores([FromQuery] int? id_org_fin = null)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            var query = "SELECT * FROM cla_organismos_financiadores WHERE activo = 'S' AND 1=1";
            var parameters = new DynamicParameters();

            if (id_org_fin.HasValue)
            {
                query += "AND id_org_fin = @id_org_fin";
                parameters.Add("id_org_fin", id_org_fin.Value);
            }

            var organismosFinanciadores = await _dbConnection.QueryAsync(query, parameters);
            var totalRegistros = organismosFinanciadores.Count();

            if (totalRegistros < 1)
            {
                return BadRequest(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontraron organismos financiadores."
                });
            }
            var objet = new List<object>();
            objet.Add(new
            {
                total_registros = totalRegistros,
                cla_organismos_financiadores = organismosFinanciadores
            });
            await _logService.LogAsync("Info", $"Usuario: {userName} Consulta Organismos Financiador", int.Parse(userId), _ip, _route, $"id_objetal: {id_org_fin}", JsonConvert.SerializeObject(objet[0]), "GET");
            return Ok(objet[0]);
        }

        [HttpPost]
        public async Task<IActionResult> InsertOrganismoFinanciadorFromExternalService([FromBody] IdcodOrgfinRequest? request)
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

            // Validar si la consulta es general
            if (request == null || string.IsNullOrEmpty(request.Idcod_orgfin))
            {
                var responseJson = new List<object>(); // Lista para acumular los resultados de las iteraciones

                // Construir la URL con los parámetros requeridos
                string urlFull = $"https://localhost:6100/api/clasificadores/sigeft/OrganismoFinanciador";

                // Consumir el servicio externo
                var organismoFinanciadoresResponse = await _externalApiService.GetOrganismosFinanciadoresAsync(urlFull, token);

                if (organismoFinanciadoresResponse != null && organismoFinanciadoresResponse.datos != null)
                {
                    foreach (var organizmo in organismoFinanciadoresResponse.datos)
                    {
                        try
                        {
                            // Validar si ya existe el registro en la base de datos
                            var existingOrganismo = _dbConnection.QueryFirstOrDefault("SELECT * FROM vw_cla_organismos_financiadores WHERE cod_grupo = @cod_grupo AND cod_subgrp = @cod_subgrp AND cod_org_fin = @cod_org_fin",
                                new
                                {
                                    cod_grupo = organizmo.cod_grupo,
                                    cod_subgrp = organizmo.cod_sub_grupo,
                                    cod_org_fin = organizmo.cod_org_fin
                                });

                            if (existingOrganismo != null)
                            {
                                // Actualizar el registro si ya existe
                                var parametros = new DynamicParameters();
                                parametros.Add("id_org_fin", existingOrganismo.id_org_fin);
                                parametros.Add("id_version", 1); // Validar este campo
                                parametros.Add("cod_grupo", organizmo.cod_grupo);
                                parametros.Add("cod_subgrp", organizmo.cod_sub_grupo);
                                parametros.Add("cod_org_fin", organizmo.cod_org_fin);
                                parametros.Add("descripcion", organizmo.descripcion_org_fin.ToUpper());
                                parametros.Add("terminal", "S");
                                parametros.Add("activo", organizmo.estado == "habilitado" ? "S" : "N");
                                parametros.Add("estado", "actualizar");
                                parametros.Add("bandeja", 0);
                                parametros.Add("usu_ins", userId);
                                parametros.Add("fec_ins", DateTime.Now);
                                parametros.Add("usu_upd", userId);
                                parametros.Add("fec_upd", DateTime.Now);

                                var returnValue = _dbConnection.QuerySingle<int>("dbo.f_cla_organismos_financiadores_upd", parametros, commandType: CommandType.StoredProcedure);

                                // Agregar la respuesta de actualización
                                responseJson.Add(new
                                {
                                    status = "update",
                                    idcod_orgfin = $"{organizmo.cod_grupo}{organizmo.cod_sub_grupo}{organizmo.cod_org_fin}",
                                    descripcion = organizmo.descripcion_org_fin.ToUpper(),
                                });
                            }
                            else
                            {
                                // Insertar el registro si no existe
                                var resultJson = _dbConnection.Execute("dbo.f_cla_organismos_financiadores_ins", new
                                {
                                    id_org_fin = 0, // Indica que deseas asignar el ID automáticamente
                                    id_version = 1, // Validar este campo
                                    cod_grupo = organizmo.cod_grupo,
                                    cod_subgrp = organizmo.cod_sub_grupo,
                                    cod_org_fin = organizmo.cod_org_fin,
                                    descripcion = organizmo.descripcion_org_fin.ToUpper(),
                                    terminal = "S",
                                    activo = organizmo.estado == "habilitado" ? "S" : "N",
                                    estado = "registrar",
                                    bandeja = 0,
                                    usu_ins = userId,
                                    fec_ins = DateTime.Now,
                                    usu_upd = userId,
                                    fec_upd = DateTime.Now,
                                }, commandType: CommandType.StoredProcedure);

                                // Agregar la respuesta de creación
                                responseJson.Add(new
                                {
                                    status = "create",
                                    idcod_orgfin = $"{organizmo.cod_grupo}{organizmo.cod_sub_grupo}{organizmo.cod_org_fin}",
                                    descripcion = organizmo.descripcion_org_fin.ToUpper(),
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Construir la entrada de error
                            responseJson.Add(new
                            {
                                status = "fail",
                                idcod_orgfin = $"{organizmo.cod_grupo}{organizmo.cod_sub_grupo}{organizmo.cod_org_fin}",
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
                        estatus_msg = "No se encontraron Organismos Financiadores para insertar."
                    });
                }

                await _logService.LogAsync("Info", $"Usuario: {userName} procesa organismos financiadores masivos", int.Parse(userId), _ip, _route, $"cod_objetal: null", $" estatus_code = 201, estatus_msg = Proceso completado con éxito., register_status = {responseJson}", "POST");
                return Ok(new
                {
                    estatus_code = "201",
                    estatus_msg = "Proceso completado con éxito.",
                    register_status = responseJson
                });
            }

            // Construir la URL con los parámetros requeridos
            string url = $"https://localhost:6100/api/clasificadores/sigeft/OrganismoFinanciador/{request.Idcod_orgfin}";

            // Consumir el servicio externo
            var organismoFinanciador = await _externalApiService.GetOrganismoFinanciadorAsync(url, token);

            if (organismoFinanciador == null )
            {
                return BadRequest(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontraron Organismos Financiadores para insertar."
                });
            }

             // Insertar en la base de datos utilizando el procedimiento almacenado
            var result = _dbConnection.Execute("dbo.f_cla_organismos_financiadores_ins", new
            {
                id_org_fin = 0, //Indica que deseas asignar el ID automáticamente
                id_version = 1, //validar este campo 
                cod_grupo = organismoFinanciador.cod_grupo,
                cod_subgrp = organismoFinanciador.cod_sub_grupo,
                cod_org_fin = organismoFinanciador.cod_org_fin,
                descripcion = organismoFinanciador.descripcion_org_fin.ToUpper(),
                terminal = "S",
                activo = organismoFinanciador.estado == "habilitado" ? "S" : "N",
                estado = "registrar",
                bandeja = 0,
                usu_ins = userId,
                fec_ins = DateTime.Now,
                usu_upd = userId,
                fec_upd = DateTime.Now,
            }, commandType: CommandType.StoredProcedure);
            await _logService.LogAsync("Info", $"Usuario: {userName} Registra Organismos Financiadores", int.Parse(userId), _ip, _route, $"cod_objetal: {request.Idcod_orgfin}", "estatus_code = 201", "POST");
            return Ok(new
            {
                estatus_code = "201",
                estatus_msg = "Organismo Financiador registrada correctamente a partir del servicio externo."
            });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatOrganismoFinanciador(int id, [FromBody] UpdateOrganismoFinanciadorResponse request)
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
                var datosExistentes = _dbConnection.QueryFirstOrDefault<dynamic>("dbo.f_cla_organismos_financiadores_leer", new
                {
                    id_org_fin = id,
                    estado = "S",
                    usu_upd = userId
                }, commandType: CommandType.StoredProcedure);

                if (datosExistentes == null)
                {
                    return NotFound(new
                    {
                        estatus_code = "404",
                        estatus_msg = "No se encontró el Organismo Financiador."
                    });
                }

                // Combinar los datos proporcionados con los datos existentes
                var id_version = request.id_version ?? datosExistentes.id_version;
                var cod_grupo = !string.IsNullOrEmpty(request.cod_grupo) ? request.cod_grupo : datosExistentes.cod_grupo;
                var cod_subgrp = !string.IsNullOrEmpty(request.cod_subgrp) ? request.cod_subgrp : datosExistentes.cod_subgrp;
                var cod_org_fin = !string.IsNullOrEmpty(request.cod_org_fin) ? request.cod_org_fin : datosExistentes.cod_org_fin;
                var descripcion = !string.IsNullOrEmpty(request.descripcion) ? request.descripcion.ToUpper() : datosExistentes.descripcion;
                var terminal = !string.IsNullOrEmpty(request.terminal) ? request.terminal : datosExistentes.terminal;
                var activo = !string.IsNullOrEmpty(request.activo) ? request.activo : datosExistentes.activo;
                var estado = "actualizar";
                var bandeja = request.bandeja ?? datosExistentes.bandeja;
                var usuIns = request.usu_ins ?? datosExistentes.usu_ins;
                var fecIns = request.fec_ins ?? datosExistentes.fec_ins;

                // Crear parámetros para el procedimiento de actualización
                var parametros = new DynamicParameters();
                parametros.Add("id_org_fin", id);
                parametros.Add("id_version", id_version);
                parametros.Add("cod_grupo", cod_grupo);
                parametros.Add("cod_subgrp", cod_subgrp);
                parametros.Add("cod_org_fin", cod_org_fin);
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
                var returnValue = _dbConnection.QuerySingle<int>("dbo.f_cla_organismos_financiadores_upd", parametros, commandType: CommandType.StoredProcedure);

                if (returnValue > 0)
                {
                    await _logService.LogAsync("Info", $"Usuario: {userName} Actualizar Funcional id: {id}", int.Parse(userId), _ip, _route, JsonConvert.SerializeObject(request), " estatus_code = 200", "PUT");
                    return Ok(new
                    {
                        estatus_code = "200",
                        estatus_msg = "Organismos financiadores actualizada correctamente."
                    });
                }

                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "No se pudo actualizar Organismos financiadores."
                });
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("Error", ex.Message + $" Usuario: {userName} actualiza Organismos financiadores id: {id} a las {DateTime.Now}", int.Parse(userId), _ip, _route, JsonConvert.SerializeObject(request), " estatus_code = 200", "PUT");
                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "Ocurrió un error al intentar actualizar Organismos financiadores.",
                    detalle_error = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrganismoFinanciador(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            // Validar si la OrganismoFinanciador existe
            var existe = _dbConnection.ExecuteScalar<int>("dbo.[f_cla_organismos_financiadores_leer]", new
            {
                id_org_fin = id,
                estado = "S",
                usu_upd = userId
            }, commandType: CommandType.StoredProcedure);

            if (existe <= 0)
            {
                return NotFound(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontró el Organismo Financiadora."
                });
            }
            try
            {
                // Ejecutar el procedimiento para eliminar fuente general
                var result = _dbConnection.Execute("dbo.[f_cla_organismos_financiadores_del]", new
                {
                    id_org_fin = id,
                    estado = "S",
                    usu_upd = userId
                }, commandType: CommandType.StoredProcedure);
                await _logService.LogAsync("Info", $"Usuario: {userName} Elimina organismos financiador id: {id}", int.Parse(userId), _ip, _route, $"id: {id}", " estatus_code = 200", "DELETE");
                return Ok(new
                {
                    estatus_code = "200",
                    estatus_msg = "Organismos financiador eliminada correctamente."
                });
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("Error", ex.Message + $" Usuario: {userName} Eliminar funcional id: {id}", int.Parse(userId), _ip, _route, $"id: {id}", " estatus_code = 200", "DELETE");
                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "Ocurrió un error al intentar Eliminar organismos financiador.",
                    detalle_error = ex.Message
                });
            }
        }

        // Clase para recibir el JSON del cliente
        public class IdcodOrgfinRequest
        {
            public string Idcod_orgfin {  get; set; }

        }
    }
}
