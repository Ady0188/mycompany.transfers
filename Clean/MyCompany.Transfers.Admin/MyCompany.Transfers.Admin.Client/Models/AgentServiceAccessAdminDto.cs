using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

public sealed class AgentServiceAccessAdminDto
{
    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = "";

    [JsonPropertyName("serviceId")]
    public string ServiceId { get; set; } = "";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("feePermille")]
    public int FeePermille { get; set; }

    [JsonPropertyName("feeFlatMinor")]
    public long FeeFlatMinor { get; set; }
}
