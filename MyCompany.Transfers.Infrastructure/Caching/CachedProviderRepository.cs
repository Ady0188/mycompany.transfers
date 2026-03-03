using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Providers;

namespace MyCompany.Transfers.Infrastructure.Caching;

public sealed class CachedProviderRepository : IProviderRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(120);
    private readonly IProviderRepository _inner;
    private readonly ICacheService _cache;
    private const string AllKey = "provider:all";

    public CachedProviderRepository(IProviderRepository inner, ICacheService cache) =>
        (_inner, _cache) = (inner, cache);

    public Task<Provider?> GetAsync(string providerId, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"provider:by-id:{providerId}", _ => _inner.GetAsync(providerId, ct), Ttl, ct);

    public Task<Provider?> GetForUpdateAsync(string providerId, CancellationToken ct) =>
        _inner.GetForUpdateAsync(providerId, ct);

    public Task<bool> ExistsAsync(string providerId, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"provider:exists:{providerId}", _ => _inner.ExistsAsync(providerId, ct), Ttl, ct);

    public Task<bool> ExistsEnabledAsync(string id, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"provider:exists-enabled:{id}", _ => _inner.ExistsEnabledAsync(id, ct), Ttl, ct);

    public async Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken ct) =>
        (await _cache.GetOrCreateAsync(AllKey, async _ => (IReadOnlyList<Provider>?)await _inner.GetAllAsync(ct), Ttl, ct))
        ?? Array.Empty<Provider>();

    public void Add(Provider provider)
    {
        _inner.Add(provider);
        var id = provider.Id;
        _ = _cache.RemoveAsync(AllKey, default);
        _ = _cache.RemoveAsync($"provider:by-id:{id}", default);
        _ = _cache.RemoveAsync($"provider:exists:{id}", default);
        _ = _cache.RemoveAsync($"provider:exists-enabled:{id}", default);
    }

    public void Update(Provider provider)
    {
        _inner.Update(provider);
        var id = provider.Id;
        _ = _cache.RemoveAsync(AllKey, default);
        _ = _cache.RemoveAsync($"provider:by-id:{id}", default);
        _ = _cache.RemoveAsync($"provider:exists:{id}", default);
        _ = _cache.RemoveAsync($"provider:exists-enabled:{id}", default);
    }

    public void Remove(Provider provider)
    {
        var id = provider.Id;
        _inner.Remove(provider);
        _ = _cache.RemoveAsync(AllKey, default);
        _ = _cache.RemoveAsync($"provider:by-id:{id}", default);
        _ = _cache.RemoveAsync($"provider:exists:{id}", default);
        _ = _cache.RemoveAsync($"provider:exists-enabled:{id}", default);
    }
}
