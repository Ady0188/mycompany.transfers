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

    private Agent() { }

    private Agent(string id, string name, string account, string timeZoneId, string settingsJson)
    {
        Id = id;
        Name = name ?? "";
        Account = account;
        TimeZoneId = timeZoneId;
        SettingsJson = settingsJson;
    }

    /// <summary>
    /// Фабрика создания агента (DDD). Гарантирует инварианты и значения по умолчанию.
    /// </summary>
    public static Agent Create(string id, string account, string? name = null, string? timeZoneId = null, string? settingsJson = null)
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
            string.IsNullOrWhiteSpace(settingsJson) ? "{}" : settingsJson);
    }

    /// <summary>
    /// Обновление профиля агента (название, счёт, часовой пояс и/или настройки). Пустые значения не меняют текущие.
    /// </summary>
    public void UpdateProfile(string? name = null, string? account = null, string? timeZoneId = null, string? settingsJson = null)
    {
        if (name != null)
            Name = name.Trim();
        if (!string.IsNullOrWhiteSpace(account))
            Account = account.Trim();
        if (!string.IsNullOrWhiteSpace(timeZoneId))
            TimeZoneId = timeZoneId;
        if (!string.IsNullOrWhiteSpace(settingsJson))
            SettingsJson = settingsJson;
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
