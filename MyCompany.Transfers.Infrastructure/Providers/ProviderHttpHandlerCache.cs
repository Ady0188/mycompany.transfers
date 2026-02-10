using MyCompany.Transfers.Domain.Providers;
using NLog;
using System.Collections.Concurrent;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MyCompany.Transfers.Infrastructure.Providers;

public sealed class ProviderHttpHandlerCache : IProviderHttpHandlerCache
{
    private readonly ConcurrentDictionary<string, HttpMessageHandler> _handlers = new();
    private Logger _logger = LogManager.GetCurrentClassLogger();

    public HttpMessageHandler GetOrCreate(string providerId, ProviderSettings settings)
    {
        return _handlers.GetOrAdd(providerId, _ =>
        {
            string certPath = string.Empty;
            string certPass = string.Empty;
            string verificationCertPath = string.Empty;
            settings?.Common.TryGetValue("requestCertPath", out certPath);
            settings?.Common.TryGetValue("requestCertPassword", out certPass);
            settings?.Common.TryGetValue("requestVerificationCertPath", out verificationCertPath);

            _logger.Info($"Creating HttpClientHandler for provider {providerId} with certPath='{certPath} {certPass}', verificationCertPath='{verificationCertPath}'");

            HttpClientHandler handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(certPath) && !string.IsNullOrEmpty(certPass))
            {
                _logger.Info($"Loading client certificate from path: {certPath}");
                X509Certificate2 certificate = new X509Certificate2(certPath, certPass);

                // Create an HttpClientHandler and configure it to use the certificate
                handler.ClientCertificates.Add(certificate);
            }

            if (!string.IsNullOrEmpty(verificationCertPath))
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // Добавь доверие вручную
                    chain.ChainPolicy.ExtraStore.Add(new X509Certificate2(verificationCertPath));
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

                    bool isValid = chain.Build(cert);
                    return isValid;
                };
            }
            else
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, SslPolicyErrors) => true;

            return handler;

            //var handler = new SocketsHttpHandler
            //{
            //    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            //    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            //};

            //if (!string.IsNullOrEmpty(certPath) && !string.IsNullOrEmpty(certPass))
            //{
            //    var clientCert = new X509Certificate2(
            //        certPath,
            //        certPass,
            //        X509KeyStorageFlags.MachineKeySet);

            //    handler.SslOptions.ClientCertificates = new X509CertificateCollection { clientCert };
            //}

            //if (!string.IsNullOrEmpty(verificationCertPath))
            //{
            //    var pinned = new X509Certificate2(verificationCertPath);

            //    handler.SslOptions.RemoteCertificateValidationCallback = (sender, cert, chain, errors) =>
            //    {
            //        if (cert is null) return false;

            //        var ok = errors == SslPolicyErrors.None;
            //        if (ok) return true;

            //        return string.Equals(cert.GetCertHashString(), pinned.GetCertHashString(),
            //            StringComparison.OrdinalIgnoreCase);
            //    };
            //}

            //return handler;
        });
    }

    public void Invalidate(string providerId)
    {
        if (_handlers.TryRemove(providerId, out var h) && h is IDisposable d)
            d.Dispose();
    }
}
