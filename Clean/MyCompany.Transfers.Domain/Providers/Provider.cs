using MyCompany.Transfers.Domain.Common;
using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Domain.Providers;

public sealed class Provider
{
    public string Id { get; private set; } = default!;
    /// <summary>Счёт для проводок (бухгалтерия).</summary>
    public string Account { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string BaseUrl { get; private set; } = default!;
    public int TimeoutSeconds { get; private set; } = 30;
    public ProviderAuthType AuthType { get; private set; } = ProviderAuthType.None;
    public string SettingsJson { get; private set; } = "{}";
    public bool IsEnabled { get; private set; } = true;
    public bool IsOnline { get; private set; }
    public int FeePermille { get; private set; }

    private Provider() { }

    public Provider(string id, string account, string name, string baseUrl, int timeoutSeconds, ProviderAuthType authType, string settingsJson, bool isEnabled = true, int feePermille = 0)
    {
        Id = id;
        if (string.IsNullOrWhiteSpace(account)) throw new DomainException("Счёт провайдера обязателен для проводок.");
        Account = account.Trim();
        Name = name;
        BaseUrl = baseUrl;
        TimeoutSeconds = timeoutSeconds;
        AuthType = authType;
        SettingsJson = settingsJson;
        IsEnabled = isEnabled;
        FeePermille = feePermille;
    }

    /// <summary>
    /// Обновление профиля провайдера. Переданные непустые значения заменяют текущие.
    /// </summary>
    public void UpdateProfile(string? account = null, string? name = null, string? baseUrl = null, int? timeoutSeconds = null,
        ProviderAuthType? authType = null, string? settingsJson = null, bool? isEnabled = null, int? feePermille = null)
    {
        if (!string.IsNullOrWhiteSpace(account)) Account = account.Trim();
        if (!string.IsNullOrWhiteSpace(name)) Name = name;
        if (!string.IsNullOrWhiteSpace(baseUrl)) BaseUrl = baseUrl;
        if (timeoutSeconds.HasValue) TimeoutSeconds = timeoutSeconds.Value;
        if (authType.HasValue) AuthType = authType.Value;
        if (!string.IsNullOrWhiteSpace(settingsJson)) SettingsJson = settingsJson;
        if (isEnabled.HasValue) IsEnabled = isEnabled.Value;
        if (feePermille.HasValue) FeePermille = feePermille.Value;
    }

    public void Disable() => IsEnabled = false;
    public void Enable() => IsEnabled = true;
    public void UpdateSettings(string settingsJson) => SettingsJson = settingsJson;
    public long CalculateFee(long amountMinor) => amountMinor * FeePermille / 10000;
}

public enum ProviderAuthType
{
    None,
    Basic,
    Bearer,
    Hamac,
    Custom
}

public sealed class ProviderSettings
{
    public Dictionary<string, ProviderOperationSettings> Operations { get; set; } = new();
    public Dictionary<OutboxStatus, string> JobScenario { get; set; } = new();
    public string JobFinalStatus { get; set; } = default!;
    public string Token { get; set; } = default!;
    public string User { get; set; } = default!;
    public string Password { get; set; } = default!;
    public Dictionary<string, string> Common { get; set; } = new();
}

public sealed class ProviderOperationSettings
{
    public string Method { get; set; } = "POST";
    public Dictionary<string, string> HeaderTemplate { get; set; } = new();
    public string PathTemplate { get; set; } = "/";
    public string? BodyTemplate { get; set; }
    public string Format { get; set; } = "json";
    public string ResponseFormat { get; set; } = "json";
    public string? ResponseField { get; set; }
    public string? SuccessField { get; set; }
    public string? SuccessValue { get; set; }
    public string? ErrorField { get; set; }
    public string? ResponseStatusPath { get; set; }
    public Dictionary<string, string>? StatusMapping { get; set; }
    public List<ProviderErrorCode> Errors { get; private set; } = new();
}
