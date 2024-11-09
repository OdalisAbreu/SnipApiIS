using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace SnipAuthServer.Services
{
    public class AuthService
    {

        private readonly IConfiguration _configuration;

        public AuthService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> AuthenticateUser(string username, string password)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand("dbo.f_usuarios_seg_estado", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@pa_cuenta_nom", username);
                    command.Parameters.AddWithValue("@pa_cuenta_pas", password);

                    connection.Open();
                    string result = (string)await command.ExecuteScalarAsync();
                    connection.Close();

                    return result;
                }
            }

        }
    }
 }
