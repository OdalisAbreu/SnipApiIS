using Dapper;
using Microsoft.AspNetCore.Mvc;
using InstitucionalSIGEF.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using InstitucionalSIGEF.Models;

namespace InstitucionalSIGEF.Controllers
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
        public async Task<IActionResult> InsertClasificadoresFromExternalService()
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

            string urlFull = $"https://localhost:6100/api/clasificadores/sigeft/ClasificadorInstitucional";

            try
            {
                // Consumir el servicio externo
                var externalResponse = await _externalApiService.GetClasificadoresAsync(urlFull, token);

                if (externalResponse == null || externalResponse.Count == 0)
                {
                    return NotFound(new
                    {
                        estatus_code = "404",
                        estatus_msg = "No se encontraron clasificadores institucionales en el servicio externo."
                    });
                }

                var responseStatus = new List<object>();

                foreach (var clasificador in externalResponse)
                {
                    try
                    {
                        // Verificar si el clasificador ya existe
                        var existingClasificador = _dbConnection.QueryFirstOrDefault("SELECT * FROM CLA_INSTITUCIONES_SIGEF WHERE id_institucion = @id_institucion",
                            new { id_institucion = clasificador.id_institucion });

                        if (existingClasificador != null)
                        {
                            // Actualizar el clasificador existente
                            var updateParameters = new DynamicParameters();
                            updateParameters.Add("id_institucion", clasificador.id_institucion);
                            updateParameters.Add("desc_institucion", clasificador.desc_institucion?.ToUpper());
                            updateParameters.Add("sigla", clasificador.sigla?.ToUpper());
                            updateParameters.Add("cod_sector", clasificador.cod_sector);
                            updateParameters.Add("cod_subsector", clasificador.cod_subsector);
                            updateParameters.Add("cod_area", clasificador.cod_area);
                            updateParameters.Add("cod_subarea", clasificador.cod_subarea);
                            updateParameters.Add("cod_seccion", clasificador.cod_seccion);
                            updateParameters.Add("cod_poderes_oe", clasificador.cod_poderes_oe);
                            updateParameters.Add("cod_entidades", clasificador.cod_entidades);
                            updateParameters.Add("cod_capitulo", clasificador.cod_capitulo);
                            updateParameters.Add("cod_subcapitulo", clasificador.cod_subcapitulo);
                            updateParameters.Add("cod_ue", clasificador.cod_ue);
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
                            insertParameters.Add("id_institucion", clasificador.id_institucion);
                            insertParameters.Add("desc_institucion", clasificador.desc_institucion?.ToUpper());
                            insertParameters.Add("sigla", clasificador.sigla?.ToUpper());
                            insertParameters.Add("cod_sector", clasificador.cod_sector);
                            insertParameters.Add("cod_subsector", clasificador.cod_subsector);
                            insertParameters.Add("cod_area", clasificador.cod_area);
                            insertParameters.Add("cod_subarea", clasificador.cod_subarea);
                            insertParameters.Add("cod_seccion", clasificador.cod_seccion);
                            insertParameters.Add("cod_poderes_oe", clasificador.cod_poderes_oe);
                            insertParameters.Add("cod_entidades", clasificador.cod_entidades);
                            insertParameters.Add("cod_capitulo", clasificador.cod_capitulo);
                            insertParameters.Add("cod_subcapitulo", clasificador.cod_subcapitulo);
                            insertParameters.Add("cod_ue", clasificador.cod_ue);
                            insertParameters.Add("activo", "S"); // Asumir que el registro está activo
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClasificadorInstitucional(int id, [FromBody] ClasificadorInstitucional request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            if (id <= 0)
            {
                return BadRequest(new { estatus_code = "400", estatus_msg = "El ID es obligatorio y debe ser mayor que cero." });
            }

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("id_clasificador", id);
                parameters.Add("descripcion", request.descripcion?.ToUpper());
                parameters.Add("activo", request.activo ? "S" : "N");
                parameters.Add("usu_upd", userId);
                parameters.Add("fec_upd", DateTime.Now);

                await _dbConnection.ExecuteAsync("dbo.F_CLA_INSTITUCIONES_SIGEF_UPD", parameters, commandType: CommandType.StoredProcedure);

                await _logService.LogAsync("Info", $"Usuario: {userName} actualizó clasificador institucional", int.Parse(userId), _ip, _route, JsonConvert.SerializeObject(request),$"estatus_code: 200" , "PUT");

                return Ok(new { estatus_code = "200", estatus_msg = "Clasificador actualizado correctamente." });
            }
            catch (Exception ex)
            {
             //   await _logService.LogAsync("Error", ex.Message, int.Parse(userId), _ip, _route, JsonConvert.SerializeObject(request), "PUT");
                return StatusCode(500, new { estatus_code = "500", estatus_msg = "Error al actualizar el clasificador.", detalle_error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClasificadorInstitucional(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("id_clasificador", id);
                parameters.Add("usu_upd", userId);

                await _dbConnection.ExecuteAsync("dbo.F_CLA_INSTITUCIONES_SIGEF_DEL", parameters, commandType: CommandType.StoredProcedure);

             //   await _logService.LogAsync("Info", $"Usuario: {userName} eliminó clasificador institucional", int.Parse(userId), _ip, _route, $"ID: {id}", "DELETE");

                return Ok(new { estatus_code = "200", estatus_msg = "Clasificador eliminado correctamente." });
            }
            catch (Exception ex)
            {
               // await _logService.LogAsync("Error", ex.Message, int.Parse(userId), _ip, _route, $"ID: {id}", "DELETE");
                return StatusCode(500, new { estatus_code = "500", estatus_msg = "Error al eliminar el clasificador.", detalle_error = ex.Message });
            }
        }

        public class ClasificadorInstitucionalRequest
        {
            public string Descripcion { get; set; }
            public bool Activo { get; set; }
        }
    }
}
