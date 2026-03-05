using Novell.Directory.Ldap;
using System.Text.RegularExpressions;

namespace MyCompany.Transfers.Api.Services;

public sealed class AdAuthService : IAdAuthService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AdAuthService> _logger;

    public AdAuthService(IConfiguration config, ILogger<AdAuthService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<(bool isValid, string? userName, string[] groups)> ValidateAsync(string login, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return (false, (string?)null, new string[] { });

        try
        {

            var ldapHost = _config["Admin:Ad:Host"]!.Trim();
            var ldapPort = _config["Admin:Ad:Port"]!.Trim();
            var baseDn = _config["Admin:Ad:BaseDn"]!.Trim();
            var domain = _config["Admin:Ad:Domain"]?.Trim();

            using var ldap = new LdapConnection();
            await ldap.ConnectAsync(ldapHost, int.Parse(ldapPort));

            // Логинимся от имени пользователя (UPN)
            await ldap.BindAsync($"{login}@{domain}", password);

            // Ищем пользователя
            var filter = $"(sAMAccountName={login})";
            var attrs = new[] { "memberOf", "displayName", "telephoneNumber" };

            var search = await ldap.SearchAsync(baseDn, LdapConnection.ScopeSub, filter, attrs, false);

            while (await search.HasMoreAsync())
            {
                var entry = await search.NextAsync();
                if (entry == null)
                    return (false, (string?)null, new string[] { });

                var attrSet = entry.GetAttributeSet();

                var displayName = attrSet?["displayName"]?.StringValue ?? string.Empty;
                var phone = attrSet?["telephoneNumber"]?.StringValue ?? string.Empty;

                var groupsDn = attrSet?["memberOf"]?.StringValueArray;

                var groups = groupsDn
                    .Select(ExtractCn)
                    .Where(cn => !string.IsNullOrWhiteSpace(cn))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return (true, displayName, groups);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AD validation failed for login {Login}", login);
        }

        return (false, (string?)null, new string[] { });
    }

    private static string? ExtractCn(string dn)
    {
        var m = Regex.Match(dn ?? "", @"CN=([^,]+)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    //public Task<(bool isValid, string? userName)> ValidateAsync1(string login, string password, CancellationToken ct = default)
    //{
    //    if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
    //        return Task.FromResult((false, (string?)null));

    //    var domain = _config["Admin:Ad:Domain"]?.Trim();
    //    try
    //    {
    //        using var ctx = string.IsNullOrEmpty(domain)
    //            ? new PrincipalContext(ContextType.Domain)
    //            : new PrincipalContext(ContextType.Domain, domain);

    //        if (ctx.ValidateCredentials(login, password))
    //        {
    //            using var user = UserPrincipal.FindByIdentity(ctx, login);
    //            var userName = user?.DisplayName ?? user?.SamAccountName ?? login;
    //            return Task.FromResult((true, (string?)userName));
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogWarning(ex, "AD validation failed for login {Login}", login);
    //    }

    //    return Task.FromResult((false, (string?)null));
    //}
}
