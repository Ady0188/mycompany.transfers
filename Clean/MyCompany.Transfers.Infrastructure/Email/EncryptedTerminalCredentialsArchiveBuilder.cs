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
        var password = GeneratePassword();
        var payload = new
        {
            terminal.Id,
            terminal.AgentId,
            terminal.Name,
            terminal.ApiKey,
            terminal.Secret,
            terminal.Active
        };
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        var ms = new MemoryStream();
        using (var zipStream = new ZipOutputStream(ms))
        {
            zipStream.SetLevel(3);
            zipStream.Password = password;

            var entry = new ZipEntry("terminal_credentials.json")
            {
                AESKeySize = 256,
                DateTime = DateTime.Now
            };
            zipStream.PutNextEntry(entry);
            zipStream.Write(jsonBytes, 0, jsonBytes.Length);
            zipStream.CloseEntry();
        }
        ms.Position = 0;
        return (ms, password);
    }
}
