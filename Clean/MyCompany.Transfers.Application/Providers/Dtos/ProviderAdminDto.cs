using MyCompany.Transfers.Domain.Providers;

namespace MyCompany.Transfers.Application.Providers.Dtos;

public sealed record ProviderAdminDto(
    string Id,
    string Account,
    string Name,
    string BaseUrl,
    int TimeoutSeconds,
    ProviderAuthType AuthType,
    string SettingsJson,
    bool IsEnabled,
    bool IsOnline,
    int FeePermille)
{
    public static ProviderAdminDto FromDomain(Provider p) =>
        new(p.Id, p.Account, p.Name, p.BaseUrl, p.TimeoutSeconds, p.AuthType, p.SettingsJson, p.IsEnabled, p.IsOnline, p.FeePermille);
}
