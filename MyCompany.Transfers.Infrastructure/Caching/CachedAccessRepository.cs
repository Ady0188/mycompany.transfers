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

    public Task<bool> AnyByAgentIdAsync(string agentId, CancellationToken ct) =>
        _inner.AnyByAgentIdAsync(agentId, ct);

    public Task<bool> AnyByServiceIdAsync(string serviceId, CancellationToken ct) =>
        _inner.AnyByServiceIdAsync(serviceId, ct);

    public Task<IReadOnlyList<AgentServiceAccess>> GetAllAgentServiceAccessAsync(CancellationToken ct) =>
        _inner.GetAllAgentServiceAccessAsync(ct);

    public Task<AgentServiceAccess?> GetAgentServiceAccessForUpdateAsync(string agentId, string serviceId, CancellationToken ct) =>
        _inner.GetAgentServiceAccessForUpdateAsync(agentId, serviceId, ct);

    public Task<bool> ExistsAgentServiceAccessAsync(string agentId, string serviceId, CancellationToken ct) =>
        _inner.ExistsAgentServiceAccessAsync(agentId, serviceId, ct);

    public void Add(AgentServiceAccess entity) => _inner.Add(entity);
    public void Update(AgentServiceAccess entity)
    {
        _inner.Update(entity);
        var agentId = entity.AgentId;
        var serviceId = entity.ServiceId;
        _ = _cache.RemoveAsync($"acc:svc:obj:{agentId}:{serviceId}", default);
        _ = _cache.RemoveAsync($"acc:svc:{agentId}:{serviceId}", default);
    }
    public void Remove(AgentServiceAccess entity)
    {
        var agentId = entity.AgentId;
        var serviceId = entity.ServiceId;
        _inner.Remove(entity);
        _ = _cache.RemoveAsync($"acc:svc:obj:{agentId}:{serviceId}", default);
        _ = _cache.RemoveAsync($"acc:svc:{agentId}:{serviceId}", default);
    }

    public Task<IReadOnlyList<AgentCurrencyAccess>> GetAllAgentCurrencyAccessAsync(CancellationToken ct) =>
        _inner.GetAllAgentCurrencyAccessAsync(ct);

    public Task<AgentCurrencyAccess?> GetAgentCurrencyAccessForUpdateAsync(string agentId, string currency, CancellationToken ct) =>
        _inner.GetAgentCurrencyAccessForUpdateAsync(agentId, currency, ct);

    public Task<bool> ExistsAgentCurrencyAccessAsync(string agentId, string currency, CancellationToken ct) =>
        _inner.ExistsAgentCurrencyAccessAsync(agentId, currency, ct);

    public void Add(AgentCurrencyAccess entity)
    {
        _inner.Add(entity);
        var agentId = entity.AgentId;
        var currency = entity.Currency.ToUpperInvariant();
        _ = _cache.RemoveAsync($"acc:ccy:{agentId}:{currency}", default);
        _ = _cache.RemoveAsync($"acc:ccy:list:{agentId}", default);
    }
    public void Update(AgentCurrencyAccess entity)
    {
        _inner.Update(entity);
        var agentId = entity.AgentId;
        var currency = entity.Currency.ToUpperInvariant();
        _ = _cache.RemoveAsync($"acc:ccy:{agentId}:{currency}", default);
        _ = _cache.RemoveAsync($"acc:ccy:list:{agentId}", default);
    }
    public void Remove(AgentCurrencyAccess entity)
    {
        var agentId = entity.AgentId;
        var currency = entity.Currency.ToUpperInvariant();
        _inner.Remove(entity);
        _ = _cache.RemoveAsync($"acc:ccy:{agentId}:{currency}", default);
        _ = _cache.RemoveAsync($"acc:ccy:list:{agentId}", default);
    }
}
