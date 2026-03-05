using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

/// <summary>Модель терминала для списка (без ApiKey и Secret).</summary>
public sealed class TerminalListDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("account")]
    public string Account { get; set; } = "";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";

    [JsonPropertyName("balanceMinor")]
    public long BalanceMinor { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; } = true;
}
