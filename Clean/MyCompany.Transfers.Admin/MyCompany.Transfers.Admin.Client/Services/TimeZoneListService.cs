using TimeZoneConverter;

namespace MyCompany.Transfers.Admin.Client.Services;

public sealed class TimeZoneListService : ITimeZoneListService
{
    private static readonly string[] DefaultIanaIds = { "UTC", "Europe/Moscow", "Asia/Dushanbe", "Asia/Tashkent", "Asia/Almaty", "Europe/London", "America/New_York" };
    private readonly List<(string Id, string DisplayName)> _list;
    private readonly IReadOnlyList<(string Id, string DisplayName)> _listRo;

    public TimeZoneListService(IConfiguration configuration)
    {
        var ianaIds = configuration.GetSection("TimeZones").Get<string[]>() ?? DefaultIanaIds;
        if (ianaIds.Length == 0)
            ianaIds = DefaultIanaIds;

        _list = new List<(string Id, string DisplayName)>();
        foreach (var ianaId in ianaIds)
        {
            if (string.IsNullOrWhiteSpace(ianaId)) continue;
            try
            {
                var tz = TZConvert.GetTimeZoneInfo(ianaId.Trim());
                var offset = tz.GetUtcOffset(DateTime.UtcNow);
                var abs = offset >= TimeSpan.Zero ? offset : -offset;
                var offsetStr = $"{(offset >= TimeSpan.Zero ? "+" : "-")}{abs.Hours:D2}:{abs.Minutes:D2}";
                var name = ianaId.Contains('/') ? ianaId[(ianaId.LastIndexOf('/') + 1)..] : ianaId;
                name = name.Replace("_", " ");
                _list.Add((ianaId.Trim(), $"{name} ({offsetStr})"));
            }
            catch
            {
                _list.Add((ianaId.Trim(), ianaId.Trim()));
            }
        }
        _list.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        _listRo = _list;
    }

    public IReadOnlyList<(string Id, string DisplayName)> GetTimeZonesForDisplay() => _listRo;

    public string GetDisplayName(string? ianaId)
    {
        if (string.IsNullOrWhiteSpace(ianaId))
            return "—";
        var found = _list.FirstOrDefault(x => string.Equals(x.Id, ianaId.Trim(), StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrEmpty(found.Id) ? ianaId.Trim() : found.DisplayName;
    }
}
