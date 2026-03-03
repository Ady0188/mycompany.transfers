using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Application.Agents.Commands;

/// <summary>
/// Дебитование баланса агента (списание). Вызывается со стороны АБС.
/// </summary>
public sealed record DebitAgentCommand(string AgentId, string Currency, long AmountMinor, long DocId) : IRequest<ErrorOr<AgentBalanceResult>>;

public sealed class DebitAgentCommandHandler : IRequestHandler<DebitAgentCommand, ErrorOr<AgentBalanceResult>>
{
    private readonly IAgentRepository _agents;
    private readonly IUnitOfWork _uow;
    private readonly IAgentBalanceHistoryRepository _balanceHistory;

    public DebitAgentCommandHandler(
        IAgentRepository agents,
        IUnitOfWork uow,
        IAgentBalanceHistoryRepository balanceHistory)
    {
        _agents = agents;
        _uow = uow;
        _balanceHistory = balanceHistory;
    }

    public async Task<ErrorOr<AgentBalanceResult>> Handle(DebitAgentCommand cmd, CancellationToken ct)
    {
        var agent = await _agents.GetForUpdateSqlAsync(cmd.AgentId, ct);
        if (agent is null)
            return AppErrors.Agents.NotFound(cmd.AgentId);

        var currency = cmd.Currency.Trim().ToUpperInvariant();
        if (cmd.AmountMinor <= 0)
            return AppErrors.Common.Validation("Сумма списания должна быть положительной.");

        try
        {
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

                agent.Debit(currency, cmd.AmountMinor);
                _agents.Update(agent);

                var newBalance = agent.Balances.TryGetValue(currency, out var updated) ? updated : currentBalance - cmd.AmountMinor;

                var history = AgentBalanceHistory.CreateForAbsDocument(
                    agent.Id,
                    cmd.DocId,
                    nowUtc,
                    currency,
                    currentBalance,
                    -cmd.AmountMinor,
                    newBalance);
                _balanceHistory.Add(history);

                return true;
            }, ct);
        }
        catch (DomainException ex) when (ex.Message.Contains("Insufficient", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Conflict(description: "Недостаточно средств на балансе агента для списания.");
        }

        var balance = agent.Balances.TryGetValue(currency, out var v) ? v : 0;
        return new AgentBalanceResult { Currency = currency, BalanceMinor = balance };
    }
}
