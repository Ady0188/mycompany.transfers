using ErrorOr;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Transfers.Dtos;
using MediatR;

namespace MyCompany.Transfers.Application.Agents.Queries;

public sealed record GetBalanceQuery(string AgentId, string? Currency)
    : IRequest<ErrorOr<BalanceResponseDto>>;

public sealed class GetBalanceQueryHandler : IRequestHandler<GetBalanceQuery, ErrorOr<BalanceResponseDto>>
{
    private readonly IAgentReadRepository _read;
    private readonly IAccessRepository _access;
    private readonly TimeProvider _clock;

    public GetBalanceQueryHandler(IAgentReadRepository read, TimeProvider clock, IAccessRepository access)
    {
        _read = read; _clock = clock;
        _access = access;
    }

    public async Task<ErrorOr<BalanceResponseDto>> Handle(GetBalanceQuery m, CancellationToken ct)
    {
        // Если указана конкретная валюта
        if (!string.IsNullOrWhiteSpace(m.Currency))
        {
            // 🔹 Проверяем доступ агента к валюте
            var hasAccess = await _access.IsCurrencyAllowedAsync(m.AgentId, m.Currency!, ct);
            if (!hasAccess)
                return AppErrors.Common.Forbidden(
                    $"Агент '{m.AgentId}' не имеет доступа к валюте '{m.Currency}'.");

            var amount = await _read.GetBalanceAsync(m.AgentId, m.Currency!, ct);
            if (amount is null)
                return AppErrors.Agents.NotFound(m.AgentId);

            return new BalanceResponseDto
            {
                AgentId = m.AgentId,
                Balances = new[]
                {
                    new MoneyDto
                    {
                        Currency = m.Currency!,
                        Amount = amount.Value
                    }
                }
            };
        }

        // 🔹 Если валюта не указана — возвращаем все доступные валюты агента
        var dto = await _read.GetBalancesAsync(m.AgentId, ct);
        if (dto is null)
            return AppErrors.Agents.NotFound(m.AgentId);

        // 🔹 Фильтруем только те валюты, к которым у агента есть доступ
        var allowed = await _access.GetAllowedCurrenciesAsync(m.AgentId, ct);
        if (allowed.Count == 0)
            return AppErrors.Common.Forbidden(
                        $"Агент '{m.AgentId}' не имеет доступныйх валют.");

        var balances = dto.Balances
            .Where(b => allowed.Contains(b.Currency, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return new BalanceResponseDto
        {
            AgentId = m.AgentId,
            Balances = balances
        };
    }
}