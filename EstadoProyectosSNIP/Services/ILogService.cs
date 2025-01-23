namespace EstadoProyectosSNIP.Services
{
    public interface ILogService
    {
        Task LogAsync(string type, string description, int userId, string? ip, string? end_point, string? input, string? output, string? method);
    }
}
