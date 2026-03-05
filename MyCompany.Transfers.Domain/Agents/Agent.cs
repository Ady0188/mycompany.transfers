using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Agents;

/// <summary>
/// Агент. Счета, валюты и балансы ведутся на уровне терминалов (один терминал = один счёт = одна валюта = один баланс).
/// </summary>
public sealed class Agent : IAggregateRoot
{
    public string Id { get; private set; } = default!;
    /// <summary>Название агента (для отображения).</summary>
    public string Name { get; private set; } = "";
    public string TimeZoneId { get; private set; } = "Asia/Dushanbe";
    public string SettingsJson { get; private set; } = "{}";
    /// <summary>Почта партнёра для отправки данных терминалов (подстановка «Кому»).</summary>
    public string? PartnerEmail { get; private set; }
    /// <summary>Локаль агента (например, "ru" или "en"). Влияет на тексты писем и прочие человекочитаемые значения.</summary>
    public string Locale { get; private set; } = "ru";

    private Agent() { }

    private Agent(string id, string name, string timeZoneId, string settingsJson, string? partnerEmail, string locale)
    {
        Id = id;
        Name = name ?? "";
        TimeZoneId = timeZoneId;
        SettingsJson = settingsJson;
        PartnerEmail = partnerEmail;
        Locale = NormalizeLocale(locale);
    }

    /// <summary>
    /// Фабрика создания агента (DDD). Счета и балансы настраиваются через терминалы.
    /// </summary>
    public static Agent Create(string id, string? name = null, string? timeZoneId = null, string? settingsJson = null, string? partnerEmail = null, string? locale = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("Id агента обязателен.");
        return new Agent(
            id,
            string.IsNullOrWhiteSpace(name) ? "" : name.Trim(),
            string.IsNullOrWhiteSpace(timeZoneId) ? "Asia/Dushanbe" : timeZoneId,
            string.IsNullOrWhiteSpace(settingsJson) ? "{}" : settingsJson,
            partnerEmail,
            locale ?? "ru");
    }

    /// <summary>
    /// Обновление профиля агента (название, часовой пояс и/или настройки). Пустые значения не меняют текущие.
    /// </summary>
    public void UpdateProfile(string? name = null, string? timeZoneId = null, string? settingsJson = null, string? partnerEmail = null, string? locale = null)
    {
        if (name != null)
            Name = name.Trim();
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
}
