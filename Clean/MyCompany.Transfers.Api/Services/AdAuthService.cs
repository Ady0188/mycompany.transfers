using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;

namespace MyCompany.Transfers.Api.Services;

/// <summary>
/// Проверка учётных данных через Active Directory (PrincipalContext).
/// Конфиг: Admin:Ad:Domain — имя домена (если пусто, используется текущий домен).
/// Только Windows.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class AdAuthService : IAdAuthService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AdAuthService> _logger;

    public AdAuthService(IConfiguration config, ILogger<AdAuthService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<(bool isValid, string? userName)> ValidateAsync(string login, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return Task.FromResult((false, (string?)null));

        var domain = _config["Admin:Ad:Domain"]?.Trim();
        try
        {
            using var ctx = string.IsNullOrEmpty(domain)
                ? new PrincipalContext(ContextType.Domain)
                : new PrincipalContext(ContextType.Domain, domain);

            if (ctx.ValidateCredentials(login, password))
            {
                using var user = UserPrincipal.FindByIdentity(ctx, login);
                var userName = user?.DisplayName ?? user?.SamAccountName ?? login;
                return Task.FromResult((true, (string?)userName));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AD validation failed for login {Login}", login);
        }

        return Task.FromResult((false, (string?)null));
    }
}
