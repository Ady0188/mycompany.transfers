using System.Text;
using System.Text.Json;
using ICSharpCode.SharpZipLib.Zip;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Infrastructure.Email;

public sealed class EncryptedTerminalCredentialsArchiveBuilder : ITerminalCredentialsArchiveBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string GeneratePassword()
    {
        const string chars = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rnd = Random.Shared;
        var len = 14;
        var buf = new char[len];
        for (var i = 0; i < len; i++)
            buf[i] = chars[rnd.Next(chars.Length)];
        return new string(buf);
    }

    public (Stream ZipStream, string GeneratedPassword) Build(Terminal terminal)
    {
        try
        {
            var password = GeneratePassword();

            var payload = new
            {
                terminal.AgentId,
                terminal.ApiKey,
                terminal.Secret
            };

            var jsonBytes = Encoding.UTF8.GetBytes(
                JsonSerializer.Serialize(payload, JsonOptions)
            );

            var ms = new MemoryStream();

            using (var zipStream = new ZipOutputStream(ms))
            {
                zipStream.IsStreamOwner = false;
                zipStream.SetLevel(3);
                zipStream.Password = password;

                var entry = new ZipEntry("credentials.json")
                {
                    AESKeySize = 256,
                    DateTime = DateTime.Now
                };

                zipStream.PutNextEntry(entry);
                zipStream.Write(jsonBytes, 0, jsonBytes.Length);
                zipStream.CloseEntry();

                zipStream.Finish();
            }

            ms.Position = 0;
            return (ms, password);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
