using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

/// <summary>Модель перевода для админ-панели (просмотр).</summary>
public sealed class TransferAdminDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("numId")]
    public long NumId { get; set; }

    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = "";

    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }

    [JsonPropertyName("terminalId")]
    public string TerminalId { get; set; } = "";

    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; } = "";

    [JsonPropertyName("serviceId")]
    public string ServiceId { get; set; } = "";

    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }

    [JsonPropertyName("providerId")]
    public string? ProviderId { get; set; }

    [JsonPropertyName("providerName")]
    public string? ProviderName { get; set; }

    [JsonPropertyName("method")]
    public int Method { get; set; }

    [JsonPropertyName("account")]
    public string Account { get; set; } = "";

    [JsonPropertyName("amountMinor")]
    public long AmountMinor { get; set; }

    [JsonPropertyName("amountCurrency")]
    public string AmountCurrency { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("providerTransferId")]
    public string? ProviderTransferId { get; set; }

    [JsonPropertyName("providerCode")]
    public string? ProviderCode { get; set; }

    [JsonPropertyName("errorDescription")]
    public string? ErrorDescription { get; set; }

    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [JsonPropertyName("preparedAtUtc")]
    public DateTimeOffset? PreparedAtUtc { get; set; }

    [JsonPropertyName("confirmedAtUtc")]
    public DateTimeOffset? ConfirmedAtUtc { get; set; }

    [JsonPropertyName("completedAtUtc")]
    public DateTimeOffset? CompletedAtUtc { get; set; }

    [JsonPropertyName("feeMinor")]
    public long FeeMinor { get; set; }

    [JsonPropertyName("feeCurrency")]
    public string FeeCurrency { get; set; } = "";

    [JsonPropertyName("creditedAmountMinor")]
    public long CreditedAmountMinor { get; set; }

    [JsonPropertyName("creditedAmountCurrency")]
    public string CreditedAmountCurrency { get; set; } = "";

    [JsonPropertyName("providerFeeMinor")]
    public long ProviderFeeMinor { get; set; }

    [JsonPropertyName("providerFeeCurrency")]
    public string ProviderFeeCurrency { get; set; } = "";

    [JsonPropertyName("exchangeRate")]
    public decimal? ExchangeRate { get; set; }
}
