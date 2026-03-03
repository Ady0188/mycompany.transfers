using System.Security.Cryptography;

namespace MyCompany.Transfers.Application.Common.Helpers;

/// <summary>Генерация криптостойкого Secret для терминала (рекомендуется 32+ символов).</summary>
public static class SecretGenerator
{
    /// <summary>Генерирует 32 байта (256 бит) случайных данных в Base64 (44 символа).</summary>
    public static string GenerateTerminalSecret()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
