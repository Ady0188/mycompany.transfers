using Microsoft.Extensions.Hosting;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Infrastructure.Encryption;

/// <summary>Инициализирует хелдер шифрования при старте приложения (чтобы value converters в EF могли его использовать).</summary>
internal sealed class CredentialsEncryptionInitializer : IHostedService
{
    private readonly IServiceProvider _sp;

    public CredentialsEncryptionInitializer(IServiceProvider sp) => _sp = sp;

    public Task StartAsync(CancellationToken ct)
    {
        try
        {
            var enc = _sp.GetService(typeof(ICredentialsEncryption)) as ICredentialsEncryption;
            if (enc != null)
                CredentialsEncryptionHolder.Encryption = enc;
        }
        catch
        {
            // Encryption optional (KeyBase64 may be not set)
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
