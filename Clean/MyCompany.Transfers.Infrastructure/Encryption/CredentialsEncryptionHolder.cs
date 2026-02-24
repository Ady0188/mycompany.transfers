using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Infrastructure.Encryption;

/// <summary>Хранит ссылку на сервис шифрования для использования в EF value converters (доступ из OnModelCreating).</summary>
internal static class CredentialsEncryptionHolder
{
    public static ICredentialsEncryption? Encryption { get; set; }
}
