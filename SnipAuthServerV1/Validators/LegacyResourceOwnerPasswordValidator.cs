using IdentityServer4.Models;
using IdentityServer4.Validation;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;

namespace SnipAuthServerV1.Validators
{
    public class LegacyResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly string _connectionString;

        public LegacyResourceOwnerPasswordValidator(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            int returnValue = -1;
            string userInfo = null;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("dbo.f_usuarios_seg_estado", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Parámetros de entrada
                        command.Parameters.AddWithValue("@pa_cuenta_nom", context.UserName);
                        command.Parameters.AddWithValue("@pa_cuenta_pas", context.Password);

                        // Parámetro de salida para el valor de retorno
                        var returnParameter = new SqlParameter("@return_value", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.ReturnValue
                        };
                        command.Parameters.Add(returnParameter);

                        // Ejecutar el procedimiento
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Capturamos el valor string principal (userFields concatenados)
                                userInfo = reader.GetString(0);
                                Console.WriteLine($"Procedure output: {userInfo}");
                            }
                        }

                        // Capturar el valor de retorno
                        returnValue = (int)returnParameter.Value;
                        Console.WriteLine($"Return value: {returnValue}");
                    }
                }
                if (returnValue != 0 || string.IsNullOrEmpty(userInfo))
                {
                    context.Result = new GrantValidationResult(
                        TokenRequestErrors.InvalidGrant,
                        "Usuario o contraseña incorrectos.");
                    Console.WriteLine("User authentication failed (invalid credentials).");
                    return;
                }

                if (!userInfo.Contains("activado"))
                {
                    context.Result = new GrantValidationResult(
                        TokenRequestErrors.InvalidGrant,
                        "El usuario no está activado o respuesta inesperada.");
                    Console.WriteLine("User not activated or unexpected SP output.");
                    return;
                }

                var userFields = userInfo.Split(';');
                if (userFields.Length < 14)
                {
                    context.Result = new GrantValidationResult(
                        TokenRequestErrors.InvalidGrant,
                        "Respuesta del SP con formato inválido.");
                    Console.WriteLine("Stored procedure output has fewer fields than expected.");
                    return;
                }

                var userId = userFields[0];
                var nombre = userFields[2];
                var username = userFields[7];

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, nombre),
                    new Claim("username", username),
                    new Claim("role", "user")
                };
                context.Result = new GrantValidationResult(
                    subject: userId,                 // Subject = ID de usuario
                    authenticationMethod: "custom",  // Nombre descriptivo
                    claims: claims
                );
                Console.WriteLine("User authenticated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during validation: {ex.Message}");
                context.Result = new GrantValidationResult(
                    TokenRequestErrors.InvalidGrant,
                    "Ocurrió un error al validar al usuario."
                );
            }
        }
    }
}
