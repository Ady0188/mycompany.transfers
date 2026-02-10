using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Infrastructure.Common.Caching;

public sealed class CachedAccessRepository : IAccessRepository
{
    private readonly IAccessRepository _inner;
    private readonly ICacheService _cache;

    public CachedAccessRepository(IAccessRepository inner, ICacheService cache)
        => (_inner, _cache) = (inner, cache);

    public Task<bool> IsServiceAllowedAsync(string agentId, string serviceId, CancellationToken ct)
        => _cache.GetOrCreateAsync(
            $"acc:svc:{agentId}:{serviceId}",
            _ => _inner.IsServiceAllowedAsync(agentId, serviceId, ct),
            TimeSpan.FromMinutes(3),
            ct);

    public Task<bool> IsCurrencyAllowedAsync(string agentId, string currency, CancellationToken ct)
        => _cache.GetOrCreateAsync(
            $"acc:ccy:{agentId}:{currency.ToUpperInvariant()}",
            _ => _inner.IsCurrencyAllowedAsync(agentId, currency, ct),
            TimeSpan.FromMinutes(3),
            ct);

    public Task<List<string>> GetAllowedCurrenciesAsync(string agentId, CancellationToken ct)
        => _cache.GetOrCreateAsync(
            $"acc:ccy:list:{agentId}",
            _ => _inner.GetAllowedCurrenciesAsync(agentId, ct),
            TimeSpan.FromMinutes(3),
            ct);

    public Task<AgentServiceAccess?> GetAgentServiceAccessAsync(string agentId, string serviceId, CancellationToken ct)
        => _cache.GetOrCreateAsync(
            $"acc:svc:obj:{agentId}:{serviceId}",
            _ => _inner.GetAgentServiceAccessAsync(agentId, serviceId, ct),
            TimeSpan.FromMinutes(3),
            ct);
}