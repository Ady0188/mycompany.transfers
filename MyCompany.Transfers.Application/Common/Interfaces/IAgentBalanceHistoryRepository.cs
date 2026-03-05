using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IAgentBalanceHistoryRepository
{
    /// <summary>По документу АБС (ищем по терминалу, т.к. один терминал = одна валюта).</summary>
    Task<AgentBalanceHistory?> GetByDocIdAsync(string terminalId, long docId, CancellationToken ct);
    /// <summary>Проверка идемпотентности по переводу (списание/возврат) по терминалу.</summary>
    Task<bool> ExistsByReferenceAsync(string terminalId, BalanceHistoryReferenceType referenceType, string referenceId, CancellationToken ct);
    void Add(AgentBalanceHistory history);
}

