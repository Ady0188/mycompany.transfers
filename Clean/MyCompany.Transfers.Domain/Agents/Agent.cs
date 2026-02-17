using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Agents;

public sealed class Agent : IAggregateRoot
{
    public string Id { get; private set; } = default!;
    public string TimeZoneId { get; private set; } = "Asia/Dushanbe";
    public Dictionary<string, long> Balances { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public string SettingsJson { get; private set; } = "{}";

    private Agent() { }

    private Agent(string id, string timeZoneId, string settingsJson)
    {
        Id = id;
        TimeZoneId = timeZoneId;
        SettingsJson = settingsJson;
    }

    /// <summary>
    /// Фабрика создания агента (DDD). Гарантирует инварианты и значения по умолчанию.
    /// </summary>
    public static Agent Create(string id, string? timeZoneId = null, string? settingsJson = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("Id агента обязателен.");
        return new Agent(
            id,
            string.IsNullOrWhiteSpace(timeZoneId) ? "Asia/Dushanbe" : timeZoneId,
            string.IsNullOrWhiteSpace(settingsJson) ? "{}" : settingsJson);
    }

    /// <summary>
    /// Обновление профиля агента (часовой пояс и/или настройки). Пустые значения не меняют текущие.
    /// </summary>
    public void UpdateProfile(string? timeZoneId = null, string? settingsJson = null)
    {
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
