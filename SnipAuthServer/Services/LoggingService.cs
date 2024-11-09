using Microsoft.Data.SqlClient;

namespace SnipAuthServer.Services
{
    public class LoggingService
    {
        private readonly IConfiguration _configuration;

        public LoggingService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task LogTransaction(string username, string status, string action, string ip)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO LogTable (Username, Status, Action, IP, Date) VALUES (@Username, @Status, @Action, @IP, @Date)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@Action", action);
                    command.Parameters.AddWithValue("@IP", ip);
                    command.Parameters.AddWithValue("@Date", DateTime.UtcNow);

                    connection.Open();
                    await command.ExecuteNonQueryAsync();
                    connection.Close();
                }
            }
        }
    }
}
