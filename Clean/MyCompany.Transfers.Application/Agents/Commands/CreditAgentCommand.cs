using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Agents.Commands;

/// <summary>
/// Кредитование баланса агента (зачисление). Вызывается со стороны АБС.
/// </summary>
public sealed record CreditAgentCommand(string AgentId, string Currency, long AmountMinor) : IRequest<ErrorOr<AgentBalanceResult>>;

public sealed class AgentBalanceResult
{
    public string Currency { get; init; } = default!;
    public long BalanceMinor { get; init; }
}

public sealed class CreditAgentCommandHandler : IRequestHandler<CreditAgentCommand, ErrorOr<AgentBalanceResult>>
{
    private readonly IAgentRepository _agents;
    private readonly IUnitOfWork _uow;

    public CreditAgentCommandHandler(IAgentRepository agents, IUnitOfWork uow)
    {
        _agents = agents;
        _uow = uow;
    }

    public async Task<ErrorOr<AgentBalanceResult>> Handle(CreditAgentCommand cmd, CancellationToken ct)
    {
        var agent = await _agents.GetForUpdateSqlAsync(cmd.AgentId, ct);
        if (agent is null)
            return AppErrors.Agents.NotFound(cmd.AgentId);

        var currency = cmd.Currency.Trim().ToUpperInvariant();
        if (cmd.AmountMinor <= 0)
            return AppErrors.Common.Validation("Сумма зачисления должна быть положительной.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            agent.Credit(currency, cmd.AmountMinor);
            _agents.Update(agent);
            return Task.FromResult(true);
        }, ct);

        var balance = agent.Balances.TryGetValue(currency, out var v) ? v : 0;
        return new AgentBalanceResult { Currency = currency, BalanceMinor = balance };
    }
}
