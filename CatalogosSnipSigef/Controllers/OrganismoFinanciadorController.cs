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
    [Route("/servicios/v1/sigef/cla/org-financiador")]
    [Authorize]
    public class OrganismoFinanciador : Controller
    {
        private readonly IDbConnection _dbConnection;
        private readonly ExternalApiService _externalApiService;
        private readonly string _urlApiBase;

        public OrganismoFinanciador(IDbConnection dbConnection, ExternalApiService externalApiService, IConfiguration configuration)
        {
            _dbConnection = dbConnection;
            _externalApiService = externalApiService;
            _urlApiBase = configuration["SigefApi:Url"];
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
            return Ok(objet[0]);
        }

        [HttpPost]
        public async Task<IActionResult> InsertOrganismoFinanciadorFromExternalService([FromBody] IdcodOrgfinRequest request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "ID desconocido";
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Nombre desconocido";

            // Validar que el campo codFteGral sea obligatorio
            if (string.IsNullOrEmpty(request.Idcod_orgfin))
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
            string url = $"https://localhost:7261/api/clasificadores/sigeft/OrganismoFinanciador/{request.Idcod_orgfin}";

            // Consumir el servicio externo
            var organismoFinanciador = await _externalApiService.GetOrganismoFinanciadorAsync(url, token);

            if (organismoFinanciador == null )
            {
                return BadRequest(new
                {
                    estatus_code = "404",
                    estatus_msg = "No se encontraron fuentes externas para insertar."
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

            return Ok(new
            {
                estatus_code = "201",
                estatus_msg = "Fuente registrada correctamente a partir del servicio externo."
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
                        estatus_msg = "No se encontró la fuente especificada."
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
                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "Ocurrió un error al intentar actualizar funcional.",
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
                    estatus_msg = "No se encontró la fuente especificada."
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

                return Ok(new
                {
                    estatus_code = "200",
                    estatus_msg = "Fuente eliminada correctamente."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    estatus_code = "500",
                    estatus_msg = "Ocurrió un error al intentar actualizar funcional.",
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
