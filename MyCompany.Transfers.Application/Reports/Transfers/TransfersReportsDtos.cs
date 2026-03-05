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
    string? AgentId = null,
    string? ProviderId = null,
    string? ServiceId = null,
    string? AmountCurrency = null);

/// <summary>Ограничения для быстрых отчетов (чтобы не нагружать БД).</summary>
public static class TransfersReportLimits
{
    /// <summary>Максимальный период в днях для одного запроса (снижает нагрузку на БД).</summary>
    public const int MaxPeriodDays = 93;
    /// <summary>Максимальный размер страницы при постраничной выборке.</summary>
    public const int MaxPageSize = 200;
    public const int DefaultPageSize = 50;
    /// <summary>Максимум строк при экспорте в файл (CSV/Excel).</summary>
    public const int ExportMaxRows = 5000;
}

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

public sealed record TransfersByBankReportItemDto(
    string BankCode,
    string BankName,
    string Currency,
    long SuccessCount,
    long SuccessAmountMinor,
    long ErrorCount,
    long ErrorAmountMinor,
    long TotalCount,
    long TotalAmountMinor);

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

