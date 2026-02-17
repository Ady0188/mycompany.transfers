using System.Collections.Concurrent;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using MyCompany.Transfers.Domain.Providers;

namespace MyCompany.Transfers.Infrastructure.Providers;

public sealed class ProviderHttpHandlerCache : IProviderHttpHandlerCache
{
    private readonly ConcurrentDictionary<string, HttpMessageHandler> _handlers = new();
    private readonly ILogger<ProviderHttpHandlerCache> _logger;

    public ProviderHttpHandlerCache(ILogger<ProviderHttpHandlerCache> logger) => _logger = logger;

    public HttpMessageHandler GetOrCreate(string providerId, ProviderSettings settings)
    {
        return _handlers.GetOrAdd(providerId, _ =>
        {
            var certPath = string.Empty;
            var certPass = string.Empty;
            var verificationCertPath = string.Empty;
            if (settings?.Common is not null)
            {
                settings.Common.TryGetValue("requestCertPath", out certPath);
                settings.Common.TryGetValue("requestCertPassword", out certPass);
                settings.Common.TryGetValue("requestVerificationCertPath", out verificationCertPath);
            }
            certPath ??= string.Empty;
            certPass ??= string.Empty;
            verificationCertPath ??= string.Empty;

            _logger.LogDebug("Creating HttpClientHandler for provider {ProviderId}", providerId);

            var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(certPath) && !string.IsNullOrEmpty(certPass))
            {
#pragma warning disable SYSLIB0057
                var certificate = new X509Certificate2(certPath, certPass);
#pragma warning restore SYSLIB0057
                handler.ClientCertificates.Add(certificate);
            }

            if (!string.IsNullOrEmpty(verificationCertPath))
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    if (cert is null || chain is null) return false;
#pragma warning disable SYSLIB0057
                    chain.ChainPolicy.ExtraStore.Add(new X509Certificate2(verificationCertPath));
#pragma warning restore SYSLIB0057
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                    return chain.Build(cert);
                };
            }
            else
            {
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            }

            return handler;
        });
    }

    public void Invalidate(string providerId)
    {
        if (_handlers.TryRemove(providerId, out var h) && h is IDisposable d)
            d.Dispose();
    }
}
