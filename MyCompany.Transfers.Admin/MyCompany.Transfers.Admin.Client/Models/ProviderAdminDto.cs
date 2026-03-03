using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

/// <summary>Модель провайдера для админ API (совпадает с API).</summary>
public sealed class ProviderAdminDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("account")]
    public string Account { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("baseUrl")]
    public string BaseUrl { get; set; } = "";

    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 30;

    [JsonPropertyName("authType")]
    public int AuthType { get; set; } // 0=None, 1=Basic, 2=Bearer, 3=Hamac, 4=Custom

    [JsonPropertyName("settingsJson")]
    public string SettingsJson { get; set; } = "{}";

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; set; }

    [JsonPropertyName("feePermille")]
    public int FeePermille { get; set; }
}
