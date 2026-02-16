using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Infrastructure.Caching;

public sealed class CachedAccessRepository : IAccessRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(3);
    private readonly IAccessRepository _inner;
    private readonly ICacheService _cache;

    public CachedAccessRepository(IAccessRepository inner, ICacheService cache) =>
        (_inner, _cache) = (inner, cache);

    public Task<AgentServiceAccess?> GetAgentServiceAccessAsync(string agentId, string serviceId, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"acc:svc:obj:{agentId}:{serviceId}", _ => _inner.GetAgentServiceAccessAsync(agentId, serviceId, ct), Ttl, ct);

    public Task<bool> IsServiceAllowedAsync(string agentId, string serviceId, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"acc:svc:{agentId}:{serviceId}", _ => _inner.IsServiceAllowedAsync(agentId, serviceId, ct), Ttl, ct);

    public Task<bool> IsCurrencyAllowedAsync(string agentId, string currency, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"acc:ccy:{agentId}:{currency.ToUpperInvariant()}", _ => _inner.IsCurrencyAllowedAsync(agentId, currency, ct), Ttl, ct);

    public async Task<List<string>> GetAllowedCurrenciesAsync(string agentId, CancellationToken ct) =>
        (await _cache.GetOrCreateAsync($"acc:ccy:list:{agentId}", async _ => (List<string>?)await _inner.GetAllowedCurrenciesAsync(agentId, ct), Ttl, ct)) ?? new List<string>();
}
