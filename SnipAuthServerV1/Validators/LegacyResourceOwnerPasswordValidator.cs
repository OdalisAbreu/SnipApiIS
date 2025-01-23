using IdentityServer4.Models;
using IdentityServer4.Validation;
using System.Data;
using System.Security.Claims;

namespace SnipAuthServerV1.Validators
{
    public class LegacyResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly IDbConnection _dbConnection;

        public LegacyResourceOwnerPasswordValidator(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            int returnValue = -1;
            string userInfo = null;

            try
            {
                // Usar la conexión inyectada (_dbConnection)
                using (var connection = _dbConnection)
                {
                    if (connection.State != ConnectionState.Open)
                    {
                        connection.Open(); // Si el proveedor no es SQL Server, usa una versión sincrónica.
                    }

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "dbo.f_usuarios_seg_estado";
                        command.CommandType = CommandType.StoredProcedure;

                        // Parámetros de entrada
                        var usernameParam = command.CreateParameter();
                        usernameParam.ParameterName = "@pa_cuenta_nom";
                        usernameParam.Value = context.UserName;
                        command.Parameters.Add(usernameParam);

                        var passwordParam = command.CreateParameter();
                        passwordParam.ParameterName = "@pa_cuenta_pas";
                        passwordParam.Value = context.Password;
                        command.Parameters.Add(passwordParam);

                        // Parámetro de salida para el valor de retorno
                        var returnParameter = command.CreateParameter();
                        returnParameter.ParameterName = "@return_value";
                        returnParameter.DbType = DbType.Int32;
                        returnParameter.Direction = ParameterDirection.ReturnValue;
                        command.Parameters.Add(returnParameter);

                        // Ejecutar el procedimiento y capturar el resultado
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Capturar el valor del procedimiento
                                userInfo = reader.GetString(0); // Asegúrate de que el índice 0 es válido
                                Console.WriteLine($"Procedure output: {userInfo}");
                            }
                        }

                        // Leer el valor del parámetro de retorno
                        returnValue = (int)returnParameter.Value;
                        Console.WriteLine($"Return value: {returnValue}");
                    }
                }

                // Validar la respuesta del procedimiento almacenado
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

                // Extraer campos relevantes de la respuesta
                var userId = userFields[0];
                var nombre = userFields[2];
                var username = userFields[7];

                // Crear los claims para el usuario autenticado
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
