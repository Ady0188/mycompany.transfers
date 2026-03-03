using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Rates.Queries;

public sealed class GetFxRateQueryHandler : IRequestHandler<GetFxRateQuery, ErrorOr<CurrencyDto>>
{
    private readonly IFxRateRepository _rates;
    private readonly IAgentReadRepository _agents;

    public GetFxRateQueryHandler(IFxRateRepository rates, IAgentReadRepository agents)
    {
        _rates = rates;
        _agents = agents;
    }

    public async Task<ErrorOr<CurrencyDto>> Handle(GetFxRateQuery m, CancellationToken ct)
    {
        if (!await _agents.ExistsAsync(m.AgentId, ct))
            return AppErrors.Agents.NotFound(m.AgentId);

        if (string.Equals(m.From, m.To, StringComparison.OrdinalIgnoreCase))
            return new CurrencyDto(m.From.ToUpperInvariant(), m.To.ToUpperInvariant(), 1m);

        var fx = await _rates.GetAsync(m.AgentId, m.From.ToUpperInvariant(), m.To.ToUpperInvariant(), ct);
        if (fx is null)
            return AppErrors.Common.NotFound($"FX rate {m.From}->{m.To} not found for agent {m.AgentId}.");

        var (rate, _) = fx.Value;
        return new CurrencyDto(m.From.ToUpperInvariant(), m.To.ToUpperInvariant(), Math.Round(rate, 4));
    }
}
