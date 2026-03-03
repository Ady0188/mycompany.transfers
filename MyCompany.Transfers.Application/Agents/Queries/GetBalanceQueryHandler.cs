using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Agents.Queries;

public sealed class GetBalanceQueryHandler : IRequestHandler<GetBalanceQuery, ErrorOr<BalanceResponseDto>>
{
    private readonly IAgentReadRepository _read;
    private readonly IAccessRepository _access;

    public GetBalanceQueryHandler(IAgentReadRepository read, IAccessRepository access)
    {
        _read = read;
        _access = access;
    }

    public async Task<ErrorOr<BalanceResponseDto>> Handle(GetBalanceQuery m, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(m.Currency))
        {
            var hasAccess = await _access.IsCurrencyAllowedAsync(m.AgentId, m.Currency!, ct);
            if (!hasAccess)
                return AppErrors.Common.Forbidden($"Агент '{m.AgentId}' не имеет доступа к валюте '{m.Currency}'.");

            var amount = await _read.GetBalanceAsync(m.AgentId, m.Currency!, ct);
            if (amount is null)
                return AppErrors.Agents.NotFound(m.AgentId);

            return new BalanceResponseDto
            {
                AgentId = m.AgentId,
                Balances = new[] { new MoneyDto { Currency = m.Currency!, Amount = amount.Value } }
            };
        }

        var dto = await _read.GetBalancesAsync(m.AgentId, ct);
        if (dto is null)
            return AppErrors.Agents.NotFound(m.AgentId);

        var allowed = await _access.GetAllowedCurrenciesAsync(m.AgentId, ct);
        if (allowed.Count == 0)
            return AppErrors.Common.Forbidden($"Агент '{m.AgentId}' не имеет доступных валют.");

        var balances = dto.Balances
            .Where(b => allowed.Contains(b.Currency, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return new BalanceResponseDto { AgentId = m.AgentId, Balances = balances };
    }
}
