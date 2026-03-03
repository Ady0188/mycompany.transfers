using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models.Reports;

public enum TransfersReportType
{
    ByPeriod,
    ByAgent,
    ByProvider,
    Revenue
}

public enum TransfersReportGroupByClient
{
    Day,
    Month
}

public sealed class TransfersReportFilterModel
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Status { get; set; }
    public string? AgentId { get; set; }
    public TransfersReportGroupByClient GroupBy { get; set; } = TransfersReportGroupByClient.Day;
}

public sealed class TransfersByPeriodReportItemModel
{
    [JsonPropertyName("periodStart")]
    public DateTime PeriodStart { get; set; }

    [JsonPropertyName("periodEnd")]
    public DateTime PeriodEnd { get; set; }

    [JsonPropertyName("transfersCount")]
    public long TransfersCount { get; set; }

    [JsonPropertyName("amountMinor")]
    public long AmountMinor { get; set; }

    [JsonPropertyName("amountCurrency")]
    public string AmountCurrency { get; set; } = "";

    [JsonPropertyName("feeMinor")]
    public long FeeMinor { get; set; }

    [JsonPropertyName("feeCurrency")]
    public string FeeCurrency { get; set; } = "";

    [JsonPropertyName("providerFeeMinor")]
    public long ProviderFeeMinor { get; set; }

    [JsonPropertyName("providerFeeCurrency")]
    public string ProviderFeeCurrency { get; set; } = "";
}

public sealed class TransfersByAgentReportItemModel
{
    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = "";

    [JsonPropertyName("agentName")]
    public string? AgentName { get; set; }

    [JsonPropertyName("transfersCount")]
    public long TransfersCount { get; set; }

    [JsonPropertyName("amountMinor")]
    public long AmountMinor { get; set; }

    [JsonPropertyName("amountCurrency")]
    public string AmountCurrency { get; set; } = "";

    [JsonPropertyName("feeMinor")]
    public long FeeMinor { get; set; }

    [JsonPropertyName("feeCurrency")]
    public string FeeCurrency { get; set; } = "";

    [JsonPropertyName("providerFeeMinor")]
    public long ProviderFeeMinor { get; set; }

    [JsonPropertyName("providerFeeCurrency")]
    public string ProviderFeeCurrency { get; set; } = "";
}

public sealed class TransfersByProviderReportItemModel
{
    [JsonPropertyName("providerId")]
    public string ProviderId { get; set; } = "";

    [JsonPropertyName("providerName")]
    public string? ProviderName { get; set; }

    [JsonPropertyName("transfersCount")]
    public long TransfersCount { get; set; }

    [JsonPropertyName("amountMinor")]
    public long AmountMinor { get; set; }

    [JsonPropertyName("amountCurrency")]
    public string AmountCurrency { get; set; } = "";

    [JsonPropertyName("feeMinor")]
    public long FeeMinor { get; set; }

    [JsonPropertyName("feeCurrency")]
    public string FeeCurrency { get; set; } = "";

    [JsonPropertyName("providerFeeMinor")]
    public long ProviderFeeMinor { get; set; }

    [JsonPropertyName("providerFeeCurrency")]
    public string ProviderFeeCurrency { get; set; } = "";
}

public sealed class TransfersRevenueReportItemModel
{
    [JsonPropertyName("periodStart")]
    public DateTime PeriodStart { get; set; }

    [JsonPropertyName("periodEnd")]
    public DateTime PeriodEnd { get; set; }

    [JsonPropertyName("transfersCount")]
    public long TransfersCount { get; set; }

    [JsonPropertyName("totalFeeMinor")]
    public long TotalFeeMinor { get; set; }

    [JsonPropertyName("totalProviderFeeMinor")]
    public long TotalProviderFeeMinor { get; set; }

    [JsonPropertyName("marginMinor")]
    public long MarginMinor { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "";
}

public sealed class TransfersReportResultModel<TItem>
{
    [JsonPropertyName("items")]
    public List<TItem> Items { get; set; } = new();

    [JsonPropertyName("totalCount")]
    public long TotalCount { get; set; }

    [JsonPropertyName("totalAmountMinor")]
    public long TotalAmountMinor { get; set; }

    [JsonPropertyName("totalFeeMinor")]
    public long TotalFeeMinor { get; set; }

    [JsonPropertyName("totalProviderFeeMinor")]
    public long TotalProviderFeeMinor { get; set; }
}

