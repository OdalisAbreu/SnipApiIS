using IdentityServer4.Models;
using IdentityServer4.Validation;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace SnipAuthServer.Validators
{
    public class CustomResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly IConfiguration _configuration;

        public CustomResourceOwnerPasswordValidator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            // Configuración de la conexión
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("dbo.f_usuarios_seg_estado", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@pa_cuenta_nom", context.UserName);
                    command.Parameters.AddWithValue("@pa_cuenta_pas", context.Password);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            string resultado = reader[0].ToString();
                            string[] userFields = resultado.Split(';');

                            if (userFields.Length >= 14)
                            {
                                var claims = new List<Claim>
                        {
                            new Claim("Id_usuario", userFields[0]),
                            new Claim("estado_usuario", userFields[1]),
                            new Claim("nombre", userFields[2]),
                            new Claim("apellido_paterno", userFields[3]),
                            new Claim("apellido_materno", userFields[4]),
                            new Claim("email", userFields[5]),
                            new Claim("usuario_activo", userFields[6]),
                            new Claim("username", userFields[7]),
                            new Claim("fecha", userFields[8]),
                            new Claim("roles", userFields[9]),
                            new Claim("es_usuario_externo", userFields[10]),
                            new Claim("id_institucion_usuario", userFields[11]),
                            new Claim("cargo", userFields[12]),
                            new Claim("es_administrador", userFields[13]),
                        };

                                context.Result = new GrantValidationResult(
                                    subject: userFields[0], // Usar ID de usuario como sujeto
                                    authenticationMethod: "custom",
                                    claims: claims // Pasar todos los claims
                                );
                            }
                            else
                            {
                                context.Result = new GrantValidationResult(
                                    TokenRequestErrors.InvalidGrant, "Resultado inesperado del procedimiento almacenado. " + resultado
                                );
                            }
                        }
                        else
                        {
                            context.Result = new GrantValidationResult(
                                TokenRequestErrors.InvalidGrant, "Usuario o contraseña incorrectos."
                            );
                        }
                    }
                }
            }
        }
    }
}
