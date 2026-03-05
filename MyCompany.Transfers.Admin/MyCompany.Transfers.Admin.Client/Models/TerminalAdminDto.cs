using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

/// <summary>Модель терминала для админ API.</summary>
public sealed class TerminalAdminDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("account")]
    public string Account { get; set; } = "";

    [JsonPropertyName("bankIncomeAccount")]
    public string? BankIncomeAccount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";

    [JsonPropertyName("balanceMinor")]
    public long BalanceMinor { get; set; }

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = "";

    [JsonPropertyName("secret")]
    public string Secret { get; set; } = "";

    [JsonPropertyName("active")]
    public bool Active { get; set; } = true;

    [JsonPropertyName("agentPartnerEmail")]
    public string? AgentPartnerEmail { get; set; }
}
