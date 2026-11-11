using System.Security.Cryptography;
using System.Text;

namespace VibeMovies.Api.Infrastructure;

/// <summary>
/// Resolve a chave de assinatura JWT: usa <c>Jwt:SecretKey</c> (config ou env <c>Jwt__SecretKey</c>) se for forte;
/// senão gera 64 bytes aleatórios UMA vez e persiste em <c>jwt.key</c> (ao lado do banco) para os tokens sobreviverem a reinícios.
/// </summary>
public static class JwtKeyProvider
{
    public const string KeyFileName = "jwt.key";

    /// <summary>Segredo antigo que ficava versionado no appsettings.json — nunca deve ser aceito.</summary>
    public const string LegacyPlaceholder = "VibeMovies_SuperSecret_Jwt_Signing_Key_2026_CinemaLovers!";

    public static byte[] ResolveSigningKey(IConfiguration configuration, string keyDirectory, ILogger? logger = null)
    {
        var configured = configuration["Jwt:SecretKey"];
        if (!string.IsNullOrWhiteSpace(configured) &&
            configured != LegacyPlaceholder &&
            !configured.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) &&
            Encoding.UTF8.GetByteCount(configured) >= 32)
        {
            return Encoding.UTF8.GetBytes(configured);
        }

        if (!string.IsNullOrWhiteSpace(configured))
            logger?.LogWarning("Jwt:SecretKey ausente, fraca (< 32 bytes) ou placeholder — usando chave local em {File}.", KeyFileName);

        Directory.CreateDirectory(keyDirectory);
        var path = Path.Combine(keyDirectory, KeyFileName);

        if (File.Exists(path))
        {
            try
            {
                var existing = Convert.FromBase64String(File.ReadAllText(path).Trim());
                if (existing.Length >= 32) return existing;
            }
            catch (FormatException)
            {
                logger?.LogWarning("Arquivo {Path} corrompido; gerando nova chave JWT (tokens antigos deixam de valer).", path);
            }
        }

        var key = RandomNumberGenerator.GetBytes(64);
        File.WriteAllText(path, Convert.ToBase64String(key));
        logger?.LogInformation("Nova chave JWT gerada em {Path}.", path);
        return key;
    }
}
