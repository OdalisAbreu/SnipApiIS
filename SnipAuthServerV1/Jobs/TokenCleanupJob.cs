namespace SnipAuthServerV1.Jobs
{
    using IdentityServer4.Stores;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class TokenCleanupJob : IHostedService, IDisposable
    {
        private readonly ILogger<TokenCleanupJob> _logger;
        private readonly IPersistedGrantStore _persistedGrantStore;
        private Timer _timer;

        public TokenCleanupJob(ILogger<TokenCleanupJob> logger, IPersistedGrantStore persistedGrantStore)
        {
            _logger = logger;
            _persistedGrantStore = persistedGrantStore;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando el job de limpieza de tokens...");
            SentrySdk.CaptureMessage("Iniciando el job de limpieza de tokens...");
            // Configurar el temporizador para ejecutarse cada 1 hora
            _timer = new Timer(CleanupTokens, null, TimeSpan.Zero, TimeSpan.FromHours(1));
            return Task.CompletedTask;
        }

        private async void CleanupTokens(object state)
        {
            try
            {
                _logger.LogInformation("Ejecutando limpieza de tokens expirados y revocados...");
                SentrySdk.CaptureMessage("Ejecutando limpieza de tokens expirados y revocados...");

               // Llamar al método para limpiar los tokens expirados
               await CleanupExpiredTokensAsync();

                _logger.LogInformation("Limpieza de tokens completada.");
                SentrySdk.CaptureMessage("Limpieza de tokens completada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la limpieza de tokens.");
                SentrySdk.CaptureException(ex);
            }
        }

        private async Task CleanupExpiredTokensAsync()
        {
            _logger.LogInformation("Obteniendo grants persistidos...");

            // Crear un filtro para obtener todos los grants
            var persistedGrants = await _persistedGrantStore.GetAllAsync(new PersistedGrantFilter());

            _logger.LogInformation($"Se encontraron {persistedGrants.Count()} grants persistidos.");

            foreach (var grant in persistedGrants)
            {
                // Verificar si el grant está expirado
                if (grant.Expiration.HasValue && grant.Expiration.Value < DateTime.UtcNow)
                {
                    _logger.LogInformation($"Eliminando token expirado: {grant.Key}");
                    SentrySdk.CaptureMessage($"Eliminando token expirado: {grant.Key}");

                    // Eliminar el grant
                    await _persistedGrantStore.RemoveAsync(grant.Key);
                }
            }

            _logger.LogInformation("Limpieza de tokens expirados completada.");
            SentrySdk.CaptureMessage("Limpieza de tokens expirados completada.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deteniendo el job de limpieza de tokens...");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
