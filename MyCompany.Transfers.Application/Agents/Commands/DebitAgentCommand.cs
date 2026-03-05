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
    private readonly ITerminalRepository _terminals;
    private readonly IUnitOfWork _uow;
    private readonly IAgentBalanceHistoryRepository _balanceHistory;

    public DebitAgentCommandHandler(
        IAgentRepository agents,
        ITerminalRepository terminals,
        IUnitOfWork uow,
        IAgentBalanceHistoryRepository balanceHistory)
    {
        _agents = agents;
        _terminals = terminals;
        _uow = uow;
        _balanceHistory = balanceHistory;
    }

    public async Task<ErrorOr<AgentBalanceResult>> Handle(DebitAgentCommand cmd, CancellationToken ct)
    {
        if (!await _agents.ExistsAsync(cmd.AgentId, ct))
            return AppErrors.Agents.NotFound(cmd.AgentId);

        var currency = cmd.Currency.Trim().ToUpperInvariant();
        if (cmd.AmountMinor <= 0)
            return AppErrors.Common.Validation("Сумма списания должна быть положительной.");

        var terminal = await _terminals.GetByAgentIdAndCurrencyForUpdateAsync(cmd.AgentId, currency, ct);
        if (terminal is null)
            return AppErrors.Common.Validation($"У агента '{cmd.AgentId}' не найден активный терминал с валютой '{currency}'.");

        try
        {
            var existingHistory = await _balanceHistory.GetByDocIdAsync(terminal.Id, cmd.DocId, ct);
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
                var currentBalance = terminal.BalanceMinor;

                terminal.Debit(cmd.AmountMinor);
                _terminals.Update(terminal);

                var newBalance = terminal.BalanceMinor;

                var history = AgentBalanceHistory.CreateForAbsDocument(
                    terminal.AgentId,
                    terminal.Id,
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

        return new AgentBalanceResult { Currency = currency, BalanceMinor = terminal.BalanceMinor };
    }
}
