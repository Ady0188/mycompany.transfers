using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Agents;

public sealed class Agent : IAggregateRoot
{
    public string Id { get; private set; } = default!;
    /// <summary>Название агента (для отображения).</summary>
    public string Name { get; private set; } = "";
    /// <summary>Счёт для проводок (бухгалтерия).</summary>
    public string Account { get; private set; } = default!;
    public string TimeZoneId { get; private set; } = "Asia/Dushanbe";
    public Dictionary<string, long> Balances { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public string SettingsJson { get; private set; } = "{}";
    /// <summary>Почта партнёра для отправки данных терминалов (подстановка «Кому»).</summary>
    public string? PartnerEmail { get; private set; }
    /// <summary>Локаль агента (например, "ru" или "en"). Влияет на тексты писем и прочие человекочитаемые значения.</summary>
    public string Locale { get; private set; } = "ru";

    private Agent() { }

    private Agent(string id, string name, string account, string timeZoneId, string settingsJson, string? partnerEmail, string locale)
    {
        Id = id;
        Name = name ?? "";
        Account = account;
        TimeZoneId = timeZoneId;
        SettingsJson = settingsJson;
        PartnerEmail = partnerEmail;
        Locale = NormalizeLocale(locale);
    }

    /// <summary>
    /// Фабрика создания агента (DDD). Гарантирует инварианты и значения по умолчанию.
    /// </summary>
    public static Agent Create(string id, string account, string? name = null, string? timeZoneId = null, string? settingsJson = null, string? partnerEmail = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("Id агента обязателен.");
        if (string.IsNullOrWhiteSpace(account))
            throw new DomainException("Счёт агента обязателен для проводок.");
        return new Agent(
            id,
            string.IsNullOrWhiteSpace(name) ? "" : name.Trim(),
            account.Trim(),
            string.IsNullOrWhiteSpace(timeZoneId) ? "Asia/Dushanbe" : timeZoneId,
            string.IsNullOrWhiteSpace(settingsJson) ? "{}" : settingsJson,
            partnerEmail,
            locale ?? "ru");
    }

    /// <summary>
    /// Обновление профиля агента (название, счёт, часовой пояс и/или настройки). Пустые значения не меняют текущие.
    /// </summary>
    public void UpdateProfile(string? name = null, string? account = null, string? timeZoneId = null, string? settingsJson = null, string? partnerEmail = null, string? locale = null)
    {
        if (name != null)
            Name = name.Trim();
        if (!string.IsNullOrWhiteSpace(account))
            Account = account.Trim();
        if (!string.IsNullOrWhiteSpace(timeZoneId))
            TimeZoneId = timeZoneId;
        if (!string.IsNullOrWhiteSpace(settingsJson))
            SettingsJson = settingsJson;
        if (partnerEmail is not null)
            PartnerEmail = partnerEmail;
        if (locale is not null)
            Locale = NormalizeLocale(locale);
    }

    private static string NormalizeLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            return "ru";
        var norm = locale.Trim().ToLowerInvariant();
        return norm switch
        {
            "en" or "en-us" or "en-gb" => "en",
            "ru" or "ru-ru" => "ru",
            _ => "ru"
        };
    }

    public bool HasSufficientBalance(string currency, long amountMinor) =>
        Balances.TryGetValue(currency, out var v) && v >= amountMinor;

    public void Credit(string currency, long minor) =>
        Balances[currency] = (Balances.TryGetValue(currency, out var v) ? v : 0) + minor;

    public void Debit(string currency, long minor)
    {
        if (!HasSufficientBalance(currency, minor))
            throw new DomainException("Insufficient balance");
        Balances[currency] -= minor;
    }
}
