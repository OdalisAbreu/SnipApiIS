using ServiciosSnip.Models;
using Microsoft.EntityFrameworkCore;
using ServiciosSnip.Models;

namespace ServiciosSnip.Services
{
    public class LogService : ILogService
    {
        private readonly ApplicationDbContext _context;

        public LogService(ApplicationDbContext  context)
        {
            _context = context;
        }

        public async Task LogAsync(string type, string description, int userId)
        {
            var log = new Log
            {
                type = type,
                description = description,
                date = DateTime.UtcNow,
                user_id = userId
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
