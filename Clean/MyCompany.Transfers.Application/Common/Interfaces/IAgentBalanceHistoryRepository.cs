using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IAgentBalanceHistoryRepository
{
    Task<AgentBalanceHistory?> GetByDocIdAsync(string agentId, string currency, long docId, CancellationToken ct);
    Task<bool> ExistsByReferenceAsync(string agentId, string currency, BalanceHistoryReferenceType referenceType, string referenceId, CancellationToken ct);
    void Add(AgentBalanceHistory history);
}

