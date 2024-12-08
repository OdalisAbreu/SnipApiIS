using Dapper;
using Microsoft.AspNetCore.Mvc;
using ServiciosSnip.Services;
using System.Data;

namespace SnipAuthServerV1.Controllers
{
    [ApiController]
    [Route("/servicios/v1/sigef/cla/org-financiador")]
    public class OrganismoFinanciador : Controller
    {
        private readonly IDbConnection _dbConnection;
        private readonly ExternalApiService _externalApiService;

        public OrganismoFinanciador(IDbConnection dbConnection, ExternalApiService externalApiService)
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
            string url = $"https://localhost:7261/api/clasificadores/sigeft/OrganismoFinanciador?estado={estado}";

            // Consumir el servicio externo
            var organismoFinanciador = await _externalApiService.GetOrganismosFinanciadoresAsync(url, token);
            if (organismoFinanciador == null )
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
                data = organismoFinanciador.datos
            });
        }

        [HttpPost]
        public async Task<IActionResult> InsertFuenteFromExternalService([FromBody] IdcodOrgfinRequest request)
        {
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
                id_version = 1,
                cod_grupo = organismoFinanciador.cod_grupo,
                cod_subgrp = organismoFinanciador.cod_sub_grupo,
                cod_org_fin = organismoFinanciador.cod_org_fin,
                descripcion = organismoFinanciador.descripcion_org_fin.ToUpper(),
                terminal = "S",
                activo = organismoFinanciador.estado == "habilitado" ? "S" : "N",
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
        public class IdcodOrgfinRequest
        {
            public string Idcod_orgfin {  get; set; }

        }
    }
}
