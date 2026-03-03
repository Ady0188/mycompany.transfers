namespace MyCompany.Transfers.Infrastructure.Encryption;

public sealed class CredentialsEncryptionOptions
{
    public const string SectionName = "CredentialsEncryption";

    /// <summary>Ключ AES-256 (32 байта) в Base64. Обязателен для шифрования ApiKey/Secret в БД.</summary>
    public string KeyBase64 { get; set; } = "";
}
