using Microsoft.Extensions.Caching.Memory;

namespace MyCompany.Transfers.Infrastructure.Common.Caching;

public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
        => _cache = cache;

    public Task<T?> GetAsync<T>(string key, CancellationToken ct)
        => Task.FromResult(
            _cache.TryGetValue(key, out T? value) ? value : default);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        _cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl,
            Size = 1
        });
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct)
    {
        if (_cache.TryGetValue(key, out T value))
            return value;

        value = await factory(ct);

        if (value is not null)
        {
            _cache.Set(key, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
                Size = 1
            });
        }

        return value!;
    }
}