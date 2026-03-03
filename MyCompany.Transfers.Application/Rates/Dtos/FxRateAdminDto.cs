using MyCompany.Transfers.Domain.Rates;

namespace MyCompany.Transfers.Application.Rates.Dtos;

public sealed record FxRateAdminDto(
    long Id,
    string AgentId,
    string BaseCurrency,
    string QuoteCurrency,
    decimal Rate,
    DateTimeOffset UpdatedAtUtc,
    string Source,
    bool IsActive)
{
    public static FxRateAdminDto FromDomain(FxRate r) =>
        new(r.Id, r.AgentId, r.BaseCurrency, r.QuoteCurrency, r.Rate, r.UpdatedAtUtc, r.Source, r.IsActive);
}
