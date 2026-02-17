using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Infrastructure.Caching;

public sealed class CachedServiceRepository : IServiceRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
    private readonly IServiceRepository _inner;
    private readonly ICacheService _cache;

    public CachedServiceRepository(IServiceRepository inner, ICacheService cache) =>
        (_inner, _cache) = (inner, cache);

    public Task<bool> ExistsAsync(string serviceId, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"service:exists:{serviceId}", _ => _inner.ExistsAsync(serviceId, ct), Ttl, ct);

    public Task<Service?> GetByIdAsync(string serviceId, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"service:byid:{serviceId}", _ => _inner.GetByIdAsync(serviceId, ct), Ttl, ct);

    public Task<Service?> GetForUpdateAsync(string serviceId, CancellationToken ct) =>
        _inner.GetForUpdateAsync(serviceId, ct);

    public Task<bool> AnyByProviderIdAsync(string providerId, CancellationToken ct) =>
        _inner.AnyByProviderIdAsync(providerId, ct);

    public Task<bool> AnyByAccountDefinitionIdAsync(Guid accountDefinitionId, CancellationToken ct) =>
        _inner.AnyByAccountDefinitionIdAsync(accountDefinitionId, ct);

    public async Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken ct) =>
        (await _cache.GetOrCreateAsync("service:all", async _ => (IReadOnlyList<Service>?)await _inner.GetAllAsync(ct), Ttl, ct)) ?? Array.Empty<Service>();

    public void Add(Service service) => _inner.Add(service);
    public void Update(Service service) => _inner.Update(service);
    public void Remove(Service service) => _inner.Remove(service);

    public async Task<(Service? Service, bool IsByPan)> GetByIdWithTypeAsync(string serviceId, CancellationToken ct)
    {
        var key = $"service:byid-withtype:{serviceId}";
        var cached = await _cache.GetAsync<ServiceWithTypeCacheDto>(key, ct);
        if (cached is not null)
            return (cached.Service, cached.IsByPan);

        var result = await _inner.GetByIdWithTypeAsync(serviceId, ct);
        if (result.Service is not null)
            await _cache.SetAsync(key, new ServiceWithTypeCacheDto(result.Service, result.IsByPan), Ttl, ct);
        return result;
    }

    private sealed record ServiceWithTypeCacheDto(Service Service, bool IsByPan);
}
