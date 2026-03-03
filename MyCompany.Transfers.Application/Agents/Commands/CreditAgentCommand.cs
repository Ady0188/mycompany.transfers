using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Agents.Commands;

/// <summary>
/// Кредитование баланса агента (зачисление). Вызывается со стороны АБС.
/// </summary>
public sealed record CreditAgentCommand(string AgentId, string Currency, long AmountMinor, long DocId) : IRequest<ErrorOr<AgentBalanceResult>>;

public sealed class AgentBalanceResult
{
    public string Currency { get; init; } = default!;
    public long BalanceMinor { get; init; }
}

public sealed class CreditAgentCommandHandler : IRequestHandler<CreditAgentCommand, ErrorOr<AgentBalanceResult>>
{
    private readonly IAgentRepository _agents;
    private readonly IUnitOfWork _uow;
    private readonly IAgentBalanceHistoryRepository _balanceHistory;

    public CreditAgentCommandHandler(
        IAgentRepository agents,
        IUnitOfWork uow,
        IAgentBalanceHistoryRepository balanceHistory)
    {
        _agents = agents;
        _uow = uow;
        _balanceHistory = balanceHistory;
    }

    public async Task<ErrorOr<AgentBalanceResult>> Handle(CreditAgentCommand cmd, CancellationToken ct)
    {
        var agent = await _agents.GetForUpdateSqlAsync(cmd.AgentId, ct);
        if (agent is null)
            return AppErrors.Agents.NotFound(cmd.AgentId);

        var currency = cmd.Currency.Trim().ToUpperInvariant();
        if (cmd.AmountMinor <= 0)
            return AppErrors.Common.Validation("Сумма зачисления должна быть положительной.");

        var existingHistory = await _balanceHistory.GetByDocIdAsync(cmd.AgentId, currency, cmd.DocId, ct);
        if (existingHistory is not null)
        {
            return new AgentBalanceResult
            {
                Currency = currency,
                BalanceMinor = existingHistory.NewBalanceMinor
            };
        }

        await _uow.ExecuteTransactionalAsync(async _ =>
        {
            var nowUtc = DateTime.UtcNow;
            var currentBalance = agent.Balances.TryGetValue(currency, out var current) ? current : 0L;

            agent.Credit(currency, cmd.AmountMinor);
            _agents.Update(agent);

            var newBalance = agent.Balances.TryGetValue(currency, out var updated) ? updated : currentBalance + cmd.AmountMinor;

            var history = AgentBalanceHistory.CreateForAbsDocument(
                agent.Id,
                cmd.DocId,
                nowUtc,
                currency,
                currentBalance,
                cmd.AmountMinor,
                newBalance);
            _balanceHistory.Add(history);

            return true;
        }, ct);

        var balance = agent.Balances.TryGetValue(currency, out var v) ? v : 0;
        return new AgentBalanceResult { Currency = currency, BalanceMinor = balance };
    }
}
