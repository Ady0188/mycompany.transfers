namespace MyCompany.Transfers.Application.Common.Interfaces;

/// <summary>Шифрование ApiKey и Secret при хранении. ApiKey — детерминированно (для поиска по ключу), Secret — с случайным IV.</summary>
public interface ICredentialsEncryption
{
    string EncryptApiKey(string plain);
    string DecryptApiKey(string encrypted);
    string EncryptSecret(string plain);
    string DecryptSecret(string encrypted);
}
