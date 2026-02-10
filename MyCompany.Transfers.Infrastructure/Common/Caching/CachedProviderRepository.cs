using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Providers;

namespace MyCompany.Transfers.Infrastructure.Common.Caching;

public sealed class CachedProviderRepository : IProviderRepository
{
    private static readonly TimeSpan ttl = TimeSpan.FromMinutes(120);

    private readonly IProviderRepository _inner;
    private readonly ICacheService _cache;

    public CachedProviderRepository(IProviderRepository inner, ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<bool> ExistsAsync(string providerId, CancellationToken ct)
        => _cache.GetOrCreateAsync(
            $"provider:exists:{providerId}",
            _ => _inner.ExistsAsync(providerId, ct),
            ttl,
            ct);

    public Task<bool> ExistsEnabledAsync(string id, CancellationToken ct)
        => _cache.GetOrCreateAsync(
            $"provider:exists-enabled:{id}",
            _ => _inner.ExistsEnabledAsync(id, ct),
            ttl,
            ct);

    public Task<Provider?> GetAsync(string providerId, CancellationToken ct)
        => _cache.GetOrCreateAsync(
            $"provider:by-id:{providerId}",
            _ => _inner.GetAsync(providerId, ct),
            ttl,
            ct);
}
