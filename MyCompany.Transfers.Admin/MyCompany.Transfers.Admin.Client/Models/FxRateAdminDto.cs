using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

public sealed class FxRateAdminDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = "";

    [JsonPropertyName("baseCurrency")]
    public string BaseCurrency { get; set; } = "";

    [JsonPropertyName("quoteCurrency")]
    public string QuoteCurrency { get; set; } = "";

    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = "manual";

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}
