using System.Collections.Concurrent;

namespace SnipAuthServer.Services
{
    public class TokenRevocationService
    {
        private readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new ConcurrentDictionary<string, DateTime>();

        // Agregar un token a la lista de revocados
        public void RevokeToken(string tokenId)
        {
            _revokedTokens[tokenId] = DateTime.UtcNow;
        }

        // Verificar si un token ha sido revocado
        public bool IsTokenRevoked(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
            {
                return false; // Si tokenId es null o vacío, considera que no está revocado
            }

            return _revokedTokens.ContainsKey(tokenId);
        }

        // Limpieza de tokens expirados (opcional)
        public void CleanUpExpiredTokens()
        {
            var expiredTokens = _revokedTokens
                .Where(x => x.Value < DateTime.UtcNow.AddHours(-1)) // Ajusta el tiempo según el tiempo de vida del token
                .Select(x => x.Key)
                .ToList();

            foreach (var tokenId in expiredTokens)
            {
                _revokedTokens.TryRemove(tokenId, out _);
            }
        }
    }
}
