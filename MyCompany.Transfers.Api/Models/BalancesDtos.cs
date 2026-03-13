using System.Text.Json.Serialization;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Api.Models;

public sealed class AgentDailyBalanceItemDto
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = "";

    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }

    [JsonPropertyName("terminalId")]
    public string TerminalId { get; set; } = "";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";

    [JsonPropertyName("openingBalanceMinor")]
    public long OpeningBalanceMinor { get; set; }

    [JsonPropertyName("closingBalanceMinor")]
    public long ClosingBalanceMinor { get; set; }

    [JsonPropertyName("timeZoneId")]
    public string TimeZoneId { get; set; } = "";

    [JsonPropertyName("scope")]
    public DailyBalanceScope Scope { get; set; }
}

public sealed class AgentBalanceHistoryItemDto
{
    [JsonPropertyName("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = "";

    [JsonPropertyName("terminalId")]
    public string TerminalId { get; set; } = "";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";

    [JsonPropertyName("currentBalanceMinor")]
    public long CurrentBalanceMinor { get; set; }

    [JsonPropertyName("newBalanceMinor")]
    public long NewBalanceMinor { get; set; }

    [JsonPropertyName("changeMinor")]
    public long ChangeMinor { get; set; }

    [JsonPropertyName("referenceType")]
    public string ReferenceType { get; set; } = "";

    [JsonPropertyName("referenceId")]
    public string ReferenceId { get; set; } = "";
}

