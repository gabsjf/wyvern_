using Microsoft.Extensions.Configuration;

namespace Wyvern.Application.Services
{
    /// <summary>
    /// Configuração do JWT lida de uma única fonte (Jwt:Key, Jwt:Issuer, Jwt:Audience, Jwt:ExpiresMinutes).
    /// Não há chave padrão: sem chave configurada a aplicação não sobe.
    /// </summary>
    public class JwtSettings
    {
        private const int MinKeyLength = 32;

        public string Key { get; init; } = string.Empty;
        public string Issuer { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public int ExpiresMinutes { get; init; }

        public static JwtSettings FromConfiguration(IConfiguration configuration)
        {
            // Jwt:Secret é aceito apenas por compatibilidade com o nome antigo da configuração.
            var key = configuration["Jwt:Key"] ?? configuration["Jwt:Secret"];

            if (string.IsNullOrWhiteSpace(key) || key.Length < MinKeyLength)
                throw new InvalidOperationException(
                    $"Configure Jwt:Key (variável de ambiente Jwt__Key) com pelo menos {MinKeyLength} caracteres.");

            return new JwtSettings
            {
                Key = key,
                Issuer = configuration["Jwt:Issuer"] ?? "WyvernApi",
                Audience = configuration["Jwt:Audience"] ?? "WyvernFront",
                ExpiresMinutes = int.TryParse(configuration["Jwt:ExpiresMinutes"], out var minutes) && minutes > 0
                    ? minutes
                    : 24 * 60
            };
        }
    }
}
