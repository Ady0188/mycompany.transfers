using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IAccessRepository
{
    Task<AgentServiceAccess?> GetAgentServiceAccessAsync(string agentId, string serviceId, CancellationToken ct);
    Task<bool> IsServiceAllowedAsync(string agentId, string serviceId, CancellationToken ct);
    Task<bool> IsCurrencyAllowedAsync(string agentId, string currency, CancellationToken ct);
    Task<List<string>> GetAllowedCurrenciesAsync(string agentId, CancellationToken ct);
}
