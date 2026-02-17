using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IAccessRepository
{
    Task<AgentServiceAccess?> GetAgentServiceAccessAsync(string agentId, string serviceId, CancellationToken ct);
    Task<bool> IsServiceAllowedAsync(string agentId, string serviceId, CancellationToken ct);
    Task<bool> IsCurrencyAllowedAsync(string agentId, string currency, CancellationToken ct);
    Task<List<string>> GetAllowedCurrenciesAsync(string agentId, CancellationToken ct);
    Task<bool> AnyByAgentIdAsync(string agentId, CancellationToken ct);
    Task<bool> AnyByServiceIdAsync(string serviceId, CancellationToken ct);

    Task<IReadOnlyList<AgentServiceAccess>> GetAllAgentServiceAccessAsync(CancellationToken ct);
    Task<AgentServiceAccess?> GetAgentServiceAccessForUpdateAsync(string agentId, string serviceId, CancellationToken ct);
    Task<bool> ExistsAgentServiceAccessAsync(string agentId, string serviceId, CancellationToken ct);
    void Add(AgentServiceAccess entity);
    void Update(AgentServiceAccess entity);
    void Remove(AgentServiceAccess entity);

    Task<IReadOnlyList<AgentCurrencyAccess>> GetAllAgentCurrencyAccessAsync(CancellationToken ct);
    Task<AgentCurrencyAccess?> GetAgentCurrencyAccessForUpdateAsync(string agentId, string currency, CancellationToken ct);
    Task<bool> ExistsAgentCurrencyAccessAsync(string agentId, string currency, CancellationToken ct);
    void Add(AgentCurrencyAccess entity);
    void Update(AgentCurrencyAccess entity);
    void Remove(AgentCurrencyAccess entity);
}
