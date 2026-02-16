using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IAgentReadRepository
{
    Task<bool> ExistsAsync(string agentId, CancellationToken ct);
    Task<Agent?> GetByIdAsync(string agentId, CancellationToken ct);
    Task<Agent?> GetForUpdateAsync(string agentId, CancellationToken ct);
    Task<Agent?> GetForUpdateSqlAsync(string agentId, CancellationToken ct);
    Task<BalanceResponseDto?> GetBalancesAsync(string agentId, CancellationToken ct);
    Task<long?> GetBalanceAsync(string agentId, string currency, CancellationToken ct);
}
