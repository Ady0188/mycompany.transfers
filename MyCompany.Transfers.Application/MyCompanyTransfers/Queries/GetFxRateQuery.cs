using ErrorOr;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Common;
using MyCompany.Transfers.Domain.Transfers.Dtos;
using MediatR;

namespace MyCompany.Transfers.Application.MyCompanyTransfers.Queries;

public sealed record GetFxRateQuery(string From, string To)
    : IRequest<ErrorOr<CurrencyDto>>;

public sealed class GetFxRateQueryHandler
    : IRequestHandler<GetFxRateQuery, ErrorOr<CurrencyDto>>
{
    private readonly IFxRateRepository _rates;
    private readonly ICurrencyCatalog _ccy; // интерфейс, дающий minorUnit (можно заглушку)

    public GetFxRateQueryHandler(IFxRateRepository rates, ICurrencyCatalog ccy)
        => (_rates, _ccy) = (rates, ccy);

    public async Task<ErrorOr<CurrencyDto>> Handle(GetFxRateQuery m, CancellationToken ct)
    {
        if (string.Equals(m.From, m.To, StringComparison.OrdinalIgnoreCase))
        {
            var mu = _ccy.GetMinorUnit(m.From);
            return new CurrencyDto(m.From.ToUpperInvariant(), m.To.ToUpperInvariant(), 1m);
        }

        var fx = await _rates.GetAsync(m.From.ToUpperInvariant(), m.To.ToUpperInvariant(), ct);
        if (fx is null)
            return AppErrors.Common.NotFound($"FX rate {m.From}->{m.To} not found.");

        var (rate, asOf) = fx.Value;
        return new CurrencyDto(
            m.From.ToUpperInvariant(), m.To.ToUpperInvariant(), Math.Round(rate, 4));
    }
}