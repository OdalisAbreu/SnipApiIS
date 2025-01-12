namespace ServiciosSnip.Services
{
    public interface ILogService
    {
        Task LogAsync(string type, string description, int userId);
    }
}
