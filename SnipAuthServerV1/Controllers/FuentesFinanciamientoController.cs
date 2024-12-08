using Dapper;
using Microsoft.AspNetCore.Mvc;
using SnipAuthServerV1.Models;
using SnipAuthServerV1.Services;
using System;
using System.Data;
using System.Threading.Tasks;


namespace SnipAuthServerV1.Controllers
{
        [ApiController]
        [Route("/servicios/v1/sigef/cla/financiamiento/fgeneral")]
    public class FuentesFinanciamientoController : Controller
    {
            private readonly IDbConnection _dbConnection;
            private readonly ExternalApiService _externalApiService;

            public FuentesFinanciamientoController(IDbConnection dbConnection, ExternalApiService externalApiService)
            {
                _dbConnection = dbConnection;
                _externalApiService = externalApiService;
            }

            [HttpGet]
            public async Task<IActionResult> GetFuentesDeFinanciamiento([FromQuery] string estado = "vigente")
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
                string url = $"https://localhost:7261/api/clasificadores/sigeft/FuentesDeFinanciamiento?estado={estado}";

                // Consumir el servicio externo
                var fuentesExternas = await _externalApiService.GetFuentesExternasAsync(url, token);

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
            public async Task<IActionResult> InsertFuenteFromExternalService([FromBody] CodFteGralRequest request)
            {
                // Validar que el campo codFteGral sea obligatorio
                if (string.IsNullOrEmpty(request.cod_fte_gral))
                {
                    return BadRequest(new
                    {
                        estatus_code = "400",
                        estatus_msg = "El campo 'cod_fte_gral' es obligatorio."
                    });
                }

            // Validar longitud mínima de cod_fte_gral
            if (string.IsNullOrEmpty(request.cod_fte_gral) || request.cod_fte_gral.Length != 7)
            {
                return BadRequest(new { mensaje = "El parámetro cod_fte_gral debe tener 7 caracteres." });
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
                string url = $"https://localhost:7261/api/clasificadores/sigeft/FuentesDeFinanciamiento/{request.cod_fte_gral}";

                // Consumir el servicio externo
                var fuenteExterna = await _externalApiService.GetFuentesFinamciamientoAsync(url, token);
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
                    usu_ins = 1,
                    fec_ins = DateTime.Now,
                    usu_upd = 1,
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

       /* [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFuente(int id)
        {

                // Validar si la fuente existe
                var existe = _dbConnection.ExecuteScalar<int>("dbo.f_cla_fuentes_generales_leer", new
                {
                    id_fte_gral = id,
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
                var result = _dbConnection.Execute("dbo.f_cla_fuentes_generales_del", new
                {
                    id_fte_gral = id,
                    estado = "S",
                    usu_upd = 1 // Usuario que realiza la acción
                }, commandType: CommandType.StoredProcedure);

                // Ejecutar el procedimiento para eliminar fuente especifica
               var resultEsp = _dbConnection.Execute("dbo.f_cla_fuentes_especificas_del", new
                {
                    id_fte_gral = id,
                    estado = "S",
                    usu_upd = 1 // Usuario que realiza la acción
                }, commandType: CommandType.StoredProcedure);

                return Ok(new
                {
                    estatus_code = "200",
                    estatus_msg = "Fuente eliminada correctamente."
                });

        }*/

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFuente(int id, [FromBody] UpdateFuenteRequest request)
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
                var datosExistentes = _dbConnection.QueryFirstOrDefault<dynamic>("dbo.f_cla_fuentes_generales_leer", new
                {
                    id_fte_gral = id,
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
                parametros.Add("usu_upd", 1);
                parametros.Add("fec_upd", DateTime.Now);

                // Ejecutar el procedimiento de actualización
                var returnValue = _dbConnection.QuerySingle<int>("dbo.f_cla_fuentes_generales_upd", parametros, commandType: CommandType.StoredProcedure);

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

        // Clase para recibir los datos de actualización
        public class UpdateFuenteRequest
        {
            public int? id_version { get; set; }
            public string? cod_fte_gral { get; set; }
            public string? descripcion { get; set; }
            public string? tipo_fuente { get; set; } // "I" o "E"
            public string? activo { get; set; } // "S" o "N"
            public string? estado { get; set; } // "actualizar", "registrado", etc.
            public int? bandeja { get; set; }
            public int? usu_ins { get; set; }
            public DateTime? fec_ins { get; set; }
        }


        // Clase para recibir el JSON del cliente
        public class CodFteGralRequest
            {
                public string cod_fte_gral { get; set; }
            }
        }
    
}
