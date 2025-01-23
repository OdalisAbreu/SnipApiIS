using Dapper;
using Microsoft.AspNetCore.Mvc;
using CatalogosSnipSigef.Services;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using System.Data;
using System.Security.Claims;
using CatalogosSnipSigef.Models;

namespace CatalogosSnipSigef.Controllers
{
    [ApiController]
    [Route("/servicios/v1/sigef/cla/institucional")]
    [Authorize]
    public class ClasificadorInstitucionalController : ControllerBase
    {
        private readonly IDbConnection _dbConnection;
        private readonly ExternalApiService _externalApiService;
        private readonly IConfiguration _configuration;
        private readonly ILogService _logService;
        private readonly string _route;
        private readonly string _ip;

        public ClasificadorInstitucionalController(IDbConnection dbConnection, ExternalApiService externalApiService, IConfiguration configuration, ILogService logService)
        {
            _dbConnection = dbConnection;
            _externalApiService = externalApiService;
            _configuration = configuration;
            _logService = logService;
            _route = "/servicios/v1/sigef/cla/institucional";
            _ip = "127.0.0.1";
        }

        [HttpGet]
        public async Task<IActionResult> GetClasificadorInstitucional([FromQuery] int? id_institucion)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            var query = "SELECT * FROM CLA_INSTITUCIONES_SIGEF WHERE 1=1";
            var parameters = new DynamicParameters();

            if (id_institucion.HasValue)
            {
                query += " AND id_institucion = @id_institucion";
                parameters.Add("id_institucion", id_institucion.Value);
            }

            var instituciones = await _dbConnection.QueryAsync(query, parameters);
            var totalRegistros = instituciones.AsList().Count;

            if (totalRegistros < 1)
            {
                return NotFound(new { estatus_code = "404", estatus_msg = "No se encontraron registros." });
            }

            await _logService.LogAsync("Info", $"Usuario: {userName} consulta clasificador institucional", int.Parse(userId), _ip, _route, $"id_fteid_institucion_gral: {id_institucion}", JsonConvert.SerializeObject(parameters),  "GET");

            return Ok(new { total_registros = totalRegistros, instituciones });
        }

        [HttpPost]
        public async Task<IActionResult> InsertClasificadoresFromExternalService([FromBody] CodInstitucion? request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";
            var token = await _externalApiService.GetAuthTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new
                {
                    estatus_code = "401",
                    estatus_msg = "No se pudo autenticar con el servicio externo."
                });
            }

            if (request == null || string.IsNullOrEmpty(request.cod_institucion)) 
            {
                var responseJson = new List<object>(); // Lista para acumular los resultados de las iteraciones
                string urlFull = $"https://localhost:6100/api/clasificadores/institucional?estado=vigente&pagina=1&tamanoPagina=10";
                try
                {
                    // Consumir el servicio externo
                    var externalResponse = await _externalApiService.GetClasificadoresAsync(urlFull, token);

                    if (externalResponse == null && externalResponse.datos == null)
                    {
                        return NotFound(new
                        {
                            estatus_code = "404",
                            estatus_msg = "No se encontraron clasificadores institucionales en el servicio externo."
                        });
                    }

                    var responseStatus = new List<object>();

                    foreach (var clasificador in externalResponse.datos)
                    {
                        try
                        {
                            // Verificar si el clasificador ya existe
                            var existingClasificadorOnly = _dbConnection.QueryFirstOrDefault("SELECT * FROM CLA_INSTITUCIONES_SIGEF WHERE sector = @sector AND subsector = @subsector AND area = @area " +
                                       "AND subarea = @subarea AND seccion = @seccion AND poderes_oe = @poderes_oe AND entidades = @entidades AND capitulo = @capitulo AND subcapitulo = @subcapitulo AND ue = @ue",
                                                  new
                                                  {
                                                      sector = clasificador.cod_sector,
                                                      subsector = clasificador.cod_subsector,
                                                      area = clasificador.cod_area,
                                                      subarea = clasificador.cod_subarea,
                                                      seccion = clasificador.cod_seccion,
                                                      poderes_oe = clasificador.cod_poderes_oe,
                                                      entidades = clasificador.cod_entidades,
                                                      capitulo = clasificador.cod_capitulo,
                                                      subcapitulo = clasificador.cod_subcapitulo,
                                                      ue = clasificador.cod_ue
                                                  });


                            if (existingClasificadorOnly != null)
                            {
                                // Actualizar el clasificador existente
                                var updateParameters = new DynamicParameters();
                                updateParameters.Add("id_institucion", existingClasificadorOnly.id_institucion);
                                updateParameters.Add("id_version", 1);
                                updateParameters.Add("cod_institucion", clasificador.id_institucion);
                                updateParameters.Add("descripcion", clasificador.desc_institucion?.ToUpper());
                                updateParameters.Add("sector", clasificador.cod_sector);
                                updateParameters.Add("subsector", clasificador.cod_subsector);
                                updateParameters.Add("area", clasificador.cod_area);
                                updateParameters.Add("subarea", clasificador.cod_subarea);
                                updateParameters.Add("seccion", clasificador.cod_seccion);
                                updateParameters.Add("poderes_oe", clasificador.cod_poderes_oe);
                                updateParameters.Add("entidades", clasificador.cod_entidades);
                                updateParameters.Add("capitulo", clasificador.cod_capitulo);
                                updateParameters.Add("subcapitulo", clasificador.cod_subcapitulo);
                                updateParameters.Add("ue", clasificador.cod_ue);
                                updateParameters.Add("estado", "actualizar");
                                updateParameters.Add("bandeja", 0);
                                updateParameters.Add("usu_ins", userId);
                                updateParameters.Add("fec_ins", DateTime.Now);
                                updateParameters.Add("usu_upd", userId);
                                updateParameters.Add("fec_upd", DateTime.Now);

                                _dbConnection.Execute("dbo.F_CLA_INSTITUCIONES_SIGEF_UPD", updateParameters, commandType: CommandType.StoredProcedure);

                                responseStatus.Add(new
                                {
                                    status = "update",
                                    id_institucion = clasificador.id_institucion,
                                    descripcion = clasificador.desc_institucion?.ToUpper()
                                });
                            }
                            else
                            {
                                // Insertar un nuevo clasificador
                                var insertParameters = new DynamicParameters();
                                insertParameters.Add("id_institucion", 0);
                                insertParameters.Add("id_version", 1);
                                insertParameters.Add("cod_institucion", clasificador.id_institucion);
                                insertParameters.Add("descripcion", clasificador.desc_institucion?.ToUpper());
                                insertParameters.Add("sector", clasificador.cod_sector);
                                insertParameters.Add("subsector", clasificador.cod_subsector);
                                insertParameters.Add("area", clasificador.cod_area);
                                insertParameters.Add("subarea", clasificador.cod_subarea);
                                insertParameters.Add("seccion", clasificador.cod_seccion);
                                insertParameters.Add("poderes_oe", clasificador.cod_poderes_oe);
                                insertParameters.Add("entidades", clasificador.cod_entidades);
                                insertParameters.Add("capitulo", clasificador.cod_capitulo);
                                insertParameters.Add("subcapitulo", clasificador.cod_subcapitulo);
                                insertParameters.Add("ue", clasificador.cod_ue);
                                insertParameters.Add("estado", "registrar");
                                insertParameters.Add("bandeja", 0);
                                insertParameters.Add("usu_ins", userId);
                                insertParameters.Add("fec_ins", DateTime.Now);
                                insertParameters.Add("usu_upd", userId);
                                insertParameters.Add("fec_upd", DateTime.Now);
                                _dbConnection.Execute("dbo.F_CLA_INSTITUCIONES_SIGEF_INS", insertParameters, commandType: CommandType.StoredProcedure);

                                responseStatus.Add(new
                                {
                                    status = "insert",
                                    id_institucion = clasificador.id_institucion,
                                    descripcion = clasificador.desc_institucion?.ToUpper()
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            responseStatus.Add(new
                            {
                                status = "error",
                                id_institucion = clasificador.id_institucion,
                                details = ex.Message
                            });
                        }
                    }

                    await _logService.LogAsync("Info", $"Usuario: {userName} procesó clasificadores institucionales", int.Parse(userId), _ip, _route, $"cod_fte_gral: NULL", JsonConvert.SerializeObject(responseStatus), "POST");

                    return Ok(new
                    {
                        estatus_code = "201",
                        estatus_msg = "Clasificadores procesados correctamente.",
                        detalles = responseStatus
                    });
                }
                catch (Exception ex)
                {
                    //  await _logService.LogAsync("Error", ex.Message, int.Parse(userId), _ip, _route, null, "POST");

                    return StatusCode(500, new
                    {
                        estatus_code = "500",
                        estatus_msg = "Ocurrió un error al procesar clasificadores institucionales.",
                        detalle_error = ex.Message
                    });
                }
            }

            string url = $"https://localhost:6100/api/clasificadores/institucional/{request.cod_institucion}";

            var clasificadorOnly = await _externalApiService.GetClasificadorAsync(url, token);

            if (clasificadorOnly == null)
            {
                return BadRequest(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontraron clasificadores institucionales para insertar."
                });
            }
            var existingClasificador = _dbConnection.QueryFirstOrDefault("SELECT * FROM CLA_INSTITUCIONES_SIGEF WHERE sector = @sector AND subsector = @subsector AND area = @area " +
                "AND subarea = @subarea AND seccion = @seccion AND poderes_oe = @poderes_oe AND entidades = @entidades AND capitulo = @capitulo AND subcapitulo = @subcapitulo AND ue = @ue",
                           new {
                               sector = clasificadorOnly.cod_sector,
                               subsector = clasificadorOnly.cod_subsector,
                               area = clasificadorOnly.cod_area,
                               subarea = clasificadorOnly.cod_subarea,
                               seccion = clasificadorOnly.cod_seccion,
                               poderes_oe = clasificadorOnly.cod_poderes_oe,
                               entidades = clasificadorOnly.cod_entidades,
                               capitulo = clasificadorOnly.cod_capitulo,
                               subcapitulo = clasificadorOnly.cod_subcapitulo,
                               ue = clasificadorOnly.cod_ue
                           });



            if (existingClasificador != null)
            {
                // Actualizar el clasificador existente
                var updateParameters = new DynamicParameters();
                updateParameters.Add("id_institucion", existingClasificador.id_institucion);
                updateParameters.Add("id_version", 1);
                updateParameters.Add("cod_institucion", clasificadorOnly.id_institucion);
                updateParameters.Add("descripcion", clasificadorOnly.desc_institucion?.ToUpper());
                updateParameters.Add("sector", clasificadorOnly.cod_sector);
                updateParameters.Add("subsector", clasificadorOnly.cod_subsector);
                updateParameters.Add("area", clasificadorOnly.cod_area);
                updateParameters.Add("subarea", clasificadorOnly.cod_subarea);
                updateParameters.Add("seccion", clasificadorOnly.cod_seccion);
                updateParameters.Add("poderes_oe", clasificadorOnly.cod_poderes_oe);
                updateParameters.Add("entidades", clasificadorOnly.cod_entidades);
                updateParameters.Add("capitulo", clasificadorOnly.cod_capitulo);
                updateParameters.Add("subcapitulo", clasificadorOnly.cod_subcapitulo);
                updateParameters.Add("ue", clasificadorOnly.cod_ue);
                updateParameters.Add("estado", "actualizar");
                updateParameters.Add("bandeja", 0);
                updateParameters.Add("usu_ins", userId);
                updateParameters.Add("fec_ins", DateTime.Now);
                updateParameters.Add("usu_upd", userId);
                updateParameters.Add("fec_upd", DateTime.Now);

                _dbConnection.Execute("dbo.F_CLA_INSTITUCIONES_SIGEF_UPD", updateParameters, commandType: CommandType.StoredProcedure);
                return Ok(new
                {
                    estatus_code = "201",
                    estatus_msg = "Clasificadores institucionales actualizdos correctamente a partir del servicio externo."
                });

            }
            else
            {
                // Insertar un nuevo clasificador
                var insertParameters = new DynamicParameters();
                insertParameters.Add("id_institucion", 0);
                insertParameters.Add("id_version", 1);
                insertParameters.Add("cod_institucion", clasificadorOnly.id_institucion);
                insertParameters.Add("descripcion", clasificadorOnly.desc_institucion?.ToUpper());
                insertParameters.Add("sector", clasificadorOnly.cod_sector);
                insertParameters.Add("subsector", clasificadorOnly.cod_subsector);
                insertParameters.Add("area", clasificadorOnly.cod_area);
                insertParameters.Add("subarea", clasificadorOnly.cod_subarea);
                insertParameters.Add("seccion", clasificadorOnly.cod_seccion);
                insertParameters.Add("poderes_oe", clasificadorOnly.cod_poderes_oe);
                insertParameters.Add("entidades", clasificadorOnly.cod_entidades);
                insertParameters.Add("capitulo", clasificadorOnly.cod_capitulo);
                insertParameters.Add("subcapitulo", clasificadorOnly.cod_subcapitulo);
                insertParameters.Add("ue", clasificadorOnly.cod_ue);
                insertParameters.Add("estado", "registrar");
                insertParameters.Add("bandeja", 0);
                insertParameters.Add("usu_ins", userId);
                insertParameters.Add("fec_ins", DateTime.Now);
                insertParameters.Add("usu_upd", userId);
                insertParameters.Add("fec_upd", DateTime.Now);
                _dbConnection.Execute("dbo.F_CLA_INSTITUCIONES_SIGEF_INS", insertParameters, commandType: CommandType.StoredProcedure);

                await _logService.LogAsync("Info", $"Usuario: {userName} Insertar clasificadores institucionales", int.Parse(userId), _ip, _route, $"cod_institucion: {request.cod_institucion}", $"estatus_code: 201, estatus_msg: Fuente registrada correctamente a partir del servicio externo.", "POST");
            }

            return Ok(new
            {
                estatus_code = "201",
                estatus_msg = "Clasificadores institucionales registrados correctamente a partir del servicio externo."
            });

        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClasificadorInstitucional(int id, [FromBody] ClasificadorInstitucional request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            if (id <= 0)
            {
                return BadRequest(new { estatus_code = "400", estatus_msg = "El ID es obligatorio y debe ser mayor que cero." });
            }

            var existingClasificador = _dbConnection.QueryFirstOrDefault("SELECT * FROM CLA_INSTITUCIONES_SIGEF WHERE id_institucion = @id_institucion",
                          new { id_institucion = id });

            if (existingClasificador != null)
            {
                try
                {
                    // Actualizar el clasificador existente
                    var updateParameters = new DynamicParameters();
                    updateParameters.Add("id_institucion", id);
                    updateParameters.Add("id_version", !string.IsNullOrEmpty(request.id_version) ? request.id_version : existingClasificador.id_version);
                    updateParameters.Add("cod_institucion", !string.IsNullOrEmpty(request.cod_institucion) ? request.cod_institucion : existingClasificador.cod_institucion);
                    updateParameters.Add("descripcion", !string.IsNullOrEmpty(request.descripcion) ? request.descripcion : existingClasificador.descripcion?.ToUpper());
                    updateParameters.Add("sector", !string.IsNullOrEmpty(request.sector) ? request.sector : existingClasificador.sector);
                    updateParameters.Add("subsector", !string.IsNullOrEmpty(request.subsector) ? request.subsector : existingClasificador.subsector);
                    updateParameters.Add("area", !string.IsNullOrEmpty(request.area) ? request.area : existingClasificador.area);
                    updateParameters.Add("subarea", !string.IsNullOrEmpty(request.subarea) ? request.subarea : existingClasificador.subarea);
                    updateParameters.Add("seccion", !string.IsNullOrEmpty(request.seccion) ? request.seccion : existingClasificador.seccion);
                    updateParameters.Add("poderes_oe", !string.IsNullOrEmpty(request.poderes_oe) ? request.poderes_oe : existingClasificador.poderes_oe);
                    updateParameters.Add("entidades", !string.IsNullOrEmpty(request.entidades) ? request.entidades : existingClasificador.entidades);
                    updateParameters.Add("capitulo", !string.IsNullOrEmpty(request.capitulo) ? request.capitulo : existingClasificador.capitulo);
                    updateParameters.Add("subcapitulo", !string.IsNullOrEmpty(request.subcapitulo) ? request.subcapitulo : existingClasificador.subcapitulo);
                    updateParameters.Add("ue", !string.IsNullOrEmpty(request.ue) ? request.ue : existingClasificador.ue);
                    updateParameters.Add("estado", "actualizar");
                    updateParameters.Add("bandeja", 0);
                    updateParameters.Add("usu_ins", userId);
                    updateParameters.Add("fec_ins", DateTime.Now);
                    updateParameters.Add("usu_upd", userId);
                    updateParameters.Add("fec_upd", DateTime.Now);

                    _dbConnection.Execute("dbo.f_cla_instituciones_sigef_upd", updateParameters, commandType: CommandType.StoredProcedure);

                    await _logService.LogAsync("Info", $"Usuario: {userName} actualizó clasificador institucional", int.Parse(userId), _ip, _route, JsonConvert.SerializeObject(request), $"estatus_code: 200", "PUT");
                    return Ok(new
                    {
                        estatus_code = "201",
                        estatus_msg = "Clasificador institucional actualizado correctamente a partir del servicio externo."
                    });
                }
                catch (Exception ex)
                {
                    await _logService.LogAsync("Error", $"erro al  actualizar clasificador institucional, Error: {ex.Message}" , int.Parse(userId), _ip, _route, JsonConvert.SerializeObject(request), $"estatus_code: 200", "PUT");//   await _logService.LogAsync("Error", ex.Message, int.Parse(userId), _ip, _route, JsonConvert.SerializeObject(request), "PUT");
                    return StatusCode(500, new { estatus_code = "500", estatus_msg = "Error al actualizar el clasificador.", detalle_error = ex.Message });
                }

            }
            else
            {
                return StatusCode(500, new
                {
                    estatus_code = "401",
                    estatus_msg = "No se encontró el clasificador institucional especificado"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClasificadorInstitucional(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            // Validar si el calsificador existe
            var result = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>("f_cla_instituciones_sigef_leer", new
            {
                id_institucion = id,
                estado = "S",
                usu_upd = userId
            }, commandType: CommandType.StoredProcedure);

            if (result == null)
            {
                return NotFound(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontró el clasificador institucional especificado."
                });
            }


            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("id_institucion", id);
                parameters.Add("usu_upd", userId);
                parameters.Add("estado", "S");

                await _dbConnection.ExecuteAsync("dbo.f_cla_instituciones_sigef_del", parameters, commandType: CommandType.StoredProcedure);

                await _logService.LogAsync("Info", $"Usuario: {userName} Elimina clasificador institucional id_clasificador: {id}", int.Parse(userId), _ip, _route, $"id: {id}", " estatus_code = 200", "DELETE");
                return Ok(new { estatus_code = "200", estatus_msg = "Clasificador eliminado correctamente." });
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("Error", ex.Message + $" Usuario: {userName} Elimina Clasificador id_clasificador: {id}", int.Parse(userId), _ip, _route, $"id: {id}", " estatus_code = 200", "DELETE");
                return StatusCode(500, new { estatus_code = "500", estatus_msg = "Error al eliminar el clasificador.", detalle_error = ex.Message });
            }
        }

        public class ClasificadorInstitucionalRequest
        {
            public string Descripcion { get; set; }
            public bool Activo { get; set; }
        }

        public class CodInstitucion
        {
            public string cod_institucion { get; set; }
        }
    }
}
