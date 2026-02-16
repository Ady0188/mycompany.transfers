using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Application.Common.Providers;

public sealed record ProviderRequest(
    string Source,
    string Operation,
    string TransferId,
    long NumId,
    string ExternalId,
    string ServiceId,
    string ProviderServiceId,
    string Account,
    long CreditAmount,
    long ProviderFee,
    string CurrencyIsoCode,
    string Proc,
    IReadOnlyDictionary<string, string>? Parameters,
    IReadOnlyDictionary<string, string>? ProvReceivedParams,
    DateTimeOffset TransferDateTime);
