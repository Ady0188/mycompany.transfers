using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Infrastructure.Encryption;

public sealed class AesCredentialsEncryption : ICredentialsEncryption
{
    private readonly byte[] _key;
    private readonly byte[] _apiKeyIv;

    public AesCredentialsEncryption(IOptions<CredentialsEncryptionOptions> options)
    {
        var keyB64 = options.Value?.KeyBase64 ?? "";
        if (string.IsNullOrWhiteSpace(keyB64) || Convert.FromBase64String(keyB64).Length != 32)
            throw new InvalidOperationException("CredentialsEncryption:KeyBase64 must be a 32-byte key in Base64.");
        _key = Convert.FromBase64String(keyB64.Trim());
        _apiKeyIv = SHA256.HashData(_key)[..16];
        CredentialsEncryptionHolder.Encryption = this;
    }

    public string EncryptApiKey(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return "";
        var bytes = Encoding.UTF8.GetBytes(plain);
        var encrypted = EncryptAes(bytes, _apiKeyIv);
        return Convert.ToBase64String(encrypted);
    }

    public string DecryptApiKey(string encrypted)
    {
        if (string.IsNullOrEmpty(encrypted)) return "";
        var bytes = Convert.FromBase64String(encrypted);
        var decrypted = DecryptAes(bytes, _apiKeyIv);
        return Encoding.UTF8.GetString(decrypted);
    }

    public string EncryptSecret(string plain)
    {
        if (string.IsNullOrEmpty(plain)) return "";
        var iv = new byte[16];
        RandomNumberGenerator.Fill(iv);
        var bytes = Encoding.UTF8.GetBytes(plain);
        var cipher = EncryptAes(bytes, iv);
        var combined = new byte[iv.Length + cipher.Length];
        Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
        Buffer.BlockCopy(cipher, 0, combined, iv.Length, cipher.Length);
        return Convert.ToBase64String(combined);
    }

    public string DecryptSecret(string encrypted)
    {
        if (string.IsNullOrEmpty(encrypted)) return "";
        var combined = Convert.FromBase64String(encrypted);
        if (combined.Length < 17) return "";
        var iv = new byte[16];
        var cipher = new byte[combined.Length - 16];
        Buffer.BlockCopy(combined, 0, iv, 0, 16);
        Buffer.BlockCopy(combined, 16, cipher, 0, cipher.Length);
        var decrypted = DecryptAes(cipher, iv);
        return Encoding.UTF8.GetString(decrypted);
    }

    private byte[] EncryptAes(byte[] plain, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        using var enc = aes.CreateEncryptor();
        return enc.TransformFinalBlock(plain, 0, plain.Length);
    }

    private byte[] DecryptAes(byte[] cipher, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        using var dec = aes.CreateDecryptor();
        return dec.TransformFinalBlock(cipher, 0, cipher.Length);
    }
}
