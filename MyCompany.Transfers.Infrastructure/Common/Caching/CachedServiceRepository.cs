using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;
using Polly.Caching;

namespace MyCompany.Transfers.Infrastructure.Common.Caching;

public sealed class CachedServiceRepository : IServiceRepository
{
    private readonly IServiceRepository _inner;
    private readonly ICacheService _cache;
    private static readonly TimeSpan ttl = TimeSpan.FromMinutes(30);
    public CachedServiceRepository(IServiceRepository inner, ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<bool> ExistsAsync(string serviceId, CancellationToken ct)
        => _cache.GetOrCreateAsync(
            $"service:exists:{serviceId}",
            _ => _inner.ExistsAsync(serviceId, ct),
            ttl,
            ct);

    public Task<Service?> GetByIdAsync(string serviceId, CancellationToken ct)
        => _cache.GetOrCreateAsync(
                $"service:byid:{serviceId}",
                _ => _inner.GetByIdAsync(serviceId, ct),
                ttl,
                ct);

    public async Task<(Service? Service, bool IsByPan)> GetByIdWithTypeAsync(string serviceId, CancellationToken ct)
    {
        var key = $"service:byid-withtype:{serviceId}";

        var cached = await _cache.GetAsync<ServiceWithTypeCacheDto>(key, ct);
        if (cached is not null)
            return (cached.Service, cached.IsByPan);

        var result = await _inner.GetByIdWithTypeAsync(serviceId, ct);

        // Кешируем только если нашли Service
        if (result.Service is not null)
        {
            await _cache.SetAsync(key,
                new ServiceWithTypeCacheDto(result.Service, result.IsByPan),
                ttl,
                ct);
        }

        return result;
    }

    private sealed record ServiceWithTypeCacheDto(Service Service, bool IsByPan);
}
