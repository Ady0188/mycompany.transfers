namespace MyCompany.Transfers.Admin.Client.Services;

/// <summary>
/// Список часовых поясов из конфига (appsettings.json → TimeZones). Изменение конфига не требует перекомпиляции.
/// </summary>
public interface ITimeZoneListService
{
    /// <summary>Список для выбора: Id (IANA), DisplayName (название и смещение).</summary>
    IReadOnlyList<(string Id, string DisplayName)> GetTimeZonesForDisplay();

    /// <summary>Отображаемое название по IANA-id, либо сам id, если не найден.</summary>
    string GetDisplayName(string? ianaId);
}
