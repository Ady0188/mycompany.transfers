using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Application.Transfers.Dtos;

/// <summary>Модель перевода для админ-панели (просмотр списка).</summary>
public sealed record TransferAdminDto(
    Guid Id,
    long NumId,
    string AgentId,
    string? AgentName,
    string TerminalId,
    string ExternalId,
    string ServiceId,
    string? ServiceName,
    string? ProviderId,
    string? ProviderName,
    int Method,
    string Account,
    long AmountMinor,
    string AmountCurrency,
    string Status,
    string? ProviderTransferId,
    string? ProviderCode,
    string? ErrorDescription,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PreparedAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    long FeeMinor,
    string FeeCurrency,
    long CreditedAmountMinor,
    string CreditedAmountCurrency,
    long ProviderFeeMinor,
    string ProviderFeeCurrency,
    decimal? ExchangeRate)
{
    public static TransferAdminDto FromDomain(Transfer t, string? agentName, string? serviceName, string? providerName)
    {
        var q = t.CurrentQuote;
        return new TransferAdminDto(
            t.Id,
            t.NumId,
            t.AgentId,
            agentName,
            t.TerminalId,
            t.ExternalId,
            t.ServiceId,
            serviceName,
            null,
            providerName,
            (int)t.Method,
            t.Account,
            t.Amount.Minor,
            t.Amount.Currency,
            t.Status.ToString(),
            t.ProviderTransferId,
            t.ProviderCode,
            t.ErrorDescription,
            t.CreatedAtUtc,
            t.PreparedAtUtc,
            t.ConfirmedAtUtc,
            t.CompletedAtUtc,
            q?.Fee.Minor ?? 0,
            q?.Fee.Currency ?? "",
            q?.CreditedAmount.Minor ?? 0,
            q?.CreditedAmount.Currency ?? "",
            q?.ProviderFee.Minor ?? 0,
            q?.ProviderFee.Currency ?? "",
            q?.ExchangeRate);
    }
}
