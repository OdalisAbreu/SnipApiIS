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
            // Obtén la cadena de conexión desde la configuración
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
                                userInfo = reader.GetString(0); // Captura el resultado principal
                                Console.WriteLine($"Procedure output: {userInfo}");
                            }
                        }

                        // Capturar el valor de retorno
                        returnValue = (int)returnParameter.Value;
                        Console.WriteLine($"Return value: {returnValue}");
                    }
                }

                // Validación del valor de retorno
                if (returnValue == 0) // 0 indica éxito en tu caso
                {
                    context.Result = new GrantValidationResult(
                        subject: context.UserName,
                        authenticationMethod: "custom",
                        claims: new[]
                        {
                            new Claim("user_info", userInfo ?? "No Info"),
                            new Claim("role", "user")
                        }
                    );
                    Console.WriteLine("User authenticated successfully.");
                }
                else
                {
                    context.Result = new GrantValidationResult(
                        TokenRequestErrors.InvalidGrant,
                        "Invalid username or password.");
                    Console.WriteLine("User authentication failed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during validation: {ex.Message}");
                context.Result = new GrantValidationResult(
                    TokenRequestErrors.InvalidGrant,
                    "An error occurred while validating the user.");
            }
        }
    }
}