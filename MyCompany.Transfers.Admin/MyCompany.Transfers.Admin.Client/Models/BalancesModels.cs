using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

public enum DailyBalanceScopeClient
{
    Local,
    Agent
}

public sealed class DailyBalanceFilterModel
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? AgentId { get; set; }
    public string? TerminalId { get; set; }
    public string? Currency { get; set; }
    public string? TimeZoneId { get; set; }
    public DailyBalanceScopeClient? Scope { get; set; }
}

public sealed class DailyBalanceItemModel
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
    public DailyBalanceScopeClient Scope { get; set; }
}

public sealed class BalanceHistoryFilterModel
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? AgentId { get; set; }
    public string? TerminalId { get; set; }
    public string? Currency { get; set; }
}

public sealed class BalanceHistoryItemModel
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

