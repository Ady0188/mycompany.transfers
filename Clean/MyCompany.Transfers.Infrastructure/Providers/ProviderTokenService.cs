using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Infrastructure.Persistence;

namespace MyCompany.Transfers.Infrastructure.Providers;

public sealed class ProviderTokenService : IProviderTokenService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> RefreshLocks = new();
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;

    public ProviderTokenService(IMemoryCache cache, IServiceScopeFactory scopeFactory)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
    }

    public async Task<string?> GetAccessTokenAsync(string providerId, CancellationToken ct)
    {
        if (_cache.TryGetValue(GetCacheKey(providerId), out TokenCacheEntry? cached) && cached is not null)
            return cached.AccessToken;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var provider = await db.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == providerId && p.IsEnabled, ct);

        if (provider is null) return null;

        var settings = DeserializeSettings(provider.SettingsJson);
        if (string.IsNullOrWhiteSpace(settings.Token))
            return null;

        PutToCache(providerId, settings.Token!, null);
        return settings.Token!;
    }

    public async Task<string> RefreshOn401Async(
        string providerId,
        Func<CancellationToken, Task<(string accessToken, DateTimeOffset? expiresAtUtc)>> loginFunc,
        CancellationToken ct)
    {
        _cache.Remove(GetCacheKey(providerId));
        var gate = RefreshLocks.GetOrAdd(providerId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await AcquireAdvisoryLockAsync(db, providerId, ct);
            try
            {
                var snap = await db.Providers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == providerId && p.IsEnabled, ct);

                if (snap is null)
                    throw new InvalidOperationException($"Provider '{providerId}' not found or disabled");

                var (newToken, _) = await loginFunc(ct);
                if (string.IsNullOrWhiteSpace(newToken))
                    throw new InvalidOperationException("loginFunc returned empty token");

                await using var tx = await db.Database.BeginTransactionAsync(ct);
                var provider = await db.Providers
                    .Where(p => p.Id == providerId)
                    .FirstOrDefaultAsync(ct);

                if (provider is null || !provider.IsEnabled)
                    throw new InvalidOperationException($"Provider '{providerId}' not found or disabled");

                var settings = DeserializeSettings(provider.SettingsJson);
                settings.Common["token"] = newToken;
                provider.UpdateSettings(JsonSerializer.Serialize(settings, JsonOptions));

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                PutToCache(providerId, newToken, null);
                return newToken;
            }
            finally
            {
                await ReleaseAdvisoryLockAsync(db, providerId, CancellationToken.None);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private static string GetCacheKey(string providerId) => $"prov:token:{providerId}";

    private void PutToCache(string providerId, string token, DateTimeOffset? expiresAtUtc)
    {
        var now = DateTimeOffset.UtcNow;
        var absoluteExpiration = expiresAtUtc ?? now.AddMinutes(2);
        _cache.Set(GetCacheKey(providerId), new TokenCacheEntry(token, expiresAtUtc),
            new MemoryCacheEntryOptions { AbsoluteExpiration = absoluteExpiration, Size = Math.Max(1, token.Length) });
    }

    private static ProviderSettings DeserializeSettings(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new ProviderSettings();
        try
        {
            return JsonSerializer.Deserialize<ProviderSettings>(json, JsonOptions) ?? new ProviderSettings();
        }
        catch
        {
            return new ProviderSettings();
        }
    }

    private static async Task AcquireAdvisoryLockAsync(AppDbContext db, string providerId, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_lock(hashtext({0})::bigint);", providerId);
    }

    private static async Task ReleaseAdvisoryLockAsync(AppDbContext db, string providerId, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_unlock(hashtext({0})::bigint);", providerId);
    }

    private sealed record TokenCacheEntry(string AccessToken, DateTimeOffset? ExpiresAtUtc);
}
