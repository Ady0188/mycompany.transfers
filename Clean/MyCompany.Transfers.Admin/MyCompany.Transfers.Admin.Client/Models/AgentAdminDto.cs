using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

/// <summary>Модель агента для админ API (совпадает с API).</summary>
public sealed class AgentAdminDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("account")]
    public string Account { get; set; } = "";

    [JsonPropertyName("timeZoneId")]
    public string TimeZoneId { get; set; } = "";

    [JsonPropertyName("settingsJson")]
    public string SettingsJson { get; set; } = "";

    [JsonPropertyName("partnerEmail")]
    public string? PartnerEmail { get; set; }
}
