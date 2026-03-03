using MyCompany.Transfers.Application.Common.Models;

namespace MyCompany.Transfers.Application.Reports.Transfers;

public enum TransfersReportGroupBy
{
    Day,
    Month
}

public sealed record TransfersCommonReportFilter(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Status = null,
    string? AgentId = null);

public sealed record TransfersByPeriodReportItemDto(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    long TransfersCount,
    long AmountMinor,
    string AmountCurrency,
    long FeeMinor,
    string FeeCurrency,
    long ProviderFeeMinor,
    string ProviderFeeCurrency);

public sealed record TransfersByAgentReportItemDto(
    string AgentId,
    string? AgentName,
    long TransfersCount,
    long AmountMinor,
    string AmountCurrency,
    long FeeMinor,
    string FeeCurrency,
    long ProviderFeeMinor,
    string ProviderFeeCurrency);

public sealed record TransfersByProviderReportItemDto(
    string ProviderId,
    string? ProviderName,
    long TransfersCount,
    long AmountMinor,
    string AmountCurrency,
    long FeeMinor,
    string FeeCurrency,
    long ProviderFeeMinor,
    string ProviderFeeCurrency);

public sealed record TransfersRevenueReportItemDto(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    long TransfersCount,
    long TotalFeeMinor,
    long TotalProviderFeeMinor,
    long MarginMinor,
    string Currency);

public sealed record TransfersReportResult<TItem>(
    IReadOnlyList<TItem> Items,
    long TotalCount,
    long TotalAmountMinor,
    long TotalFeeMinor,
    long TotalProviderFeeMinor)
{
    public static TransfersReportResult<TItem> Empty() =>
        new(Array.Empty<TItem>(), 0, 0, 0, 0);
}

