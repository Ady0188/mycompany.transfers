using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MyCompany.Transfers.Infrastructure.Providers;

internal interface IProviderTokenService
{
    Task<string?> GetAccessTokenAsync(string providerId, CancellationToken ct);

    Task<string> RefreshOn401Async(
        string providerId,
        Func<CancellationToken, Task<(string accessToken, DateTimeOffset? expiresAtUtc)>> loginFunc,
        CancellationToken ct);
}

internal sealed class ProviderTokenService : IProviderTokenService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _refreshLocks = new();

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;

    // Сколько секунд до истечения считать токен "почти истёкшим"
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromSeconds(60);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ProviderTokenService(IMemoryCache cache, IServiceScopeFactory scopeFactory)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
    }

    public async Task<string?> GetAccessTokenAsync(string providerId, CancellationToken ct)
    {
        if (_cache.TryGetValue(GetCacheKey(providerId), out TokenCacheEntry cached))
            return cached.AccessToken;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var provider = await db.Providers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == providerId && p.IsEnabled, ct);

        if (provider is null)
            return null;

        var settings = DeserializeSettings(provider.SettingsJson);
        if (string.IsNullOrWhiteSpace(settings.Token))
            return null;

        // Кладём в кэш на некоторое время (можно 1-6 часов, но лучше коротко если провайдер может отозвать)
        PutToCache(providerId, settings.Token!, expiresAtUtc: null);

        return settings.Token!;
    }

    public async Task<string> RefreshOn401Async(
        string providerId,
        Func<CancellationToken, Task<(string accessToken, DateTimeOffset? expiresAtUtc)>> loginFunc,
        CancellationToken ct)
    {
        // 401 => текущий токен плохой => чистим кэш сразу, иначе double-check вернёт старый токен
        _cache.Remove(GetCacheKey(providerId));

        var gate = _refreshLocks.GetOrAdd(providerId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);

        try
        {
            // Double-check уже БЕЗ кэша: токен мог обновить другой поток раньше, но мы кэш убрали.
            // Поэтому проверяем из БД напрямую.
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await AcquireAdvisoryLockAsync(db, providerId, ct);
            try
            {
                // Double-check #2: вдруг другой инстанс уже обновил токен
                var snap = await db.Providers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == providerId && p.IsEnabled, ct);

                if (snap is null)
                    throw new InvalidOperationException($"Provider '{providerId}' not found or disabled");

                //var snapSettings = DeserializeSettings(snap.SettingsJson);
                //if (!string.IsNullOrWhiteSpace(snapSettings.Token))
                //{
                //    // Здесь важно: если провайдер возвращает 401 на "старый" токен,
                //    // то snapSettings.Token может быть уже новым (после refresh другим потоком/инстансом).
                //    // Мы просто используем то, что лежит в БД сейчас.
                //    PutToCache(providerId, snapSettings.Token!, expiresAtUtc: null);
                //    return snapSettings.Token!;
                //}

                // Логинимся (один раз под lock)
                var (newToken, _) = await loginFunc(ct);
                if (string.IsNullOrWhiteSpace(newToken))
                    throw new InvalidOperationException("loginFunc returned empty token");

                await using var tx = await db.Database.BeginTransactionAsync(ct);

                // Строковая блокировка provider (в рамках БД)
                var provider = await db.Providers
                    .FromSqlInterpolated($@"
                    SELECT *
                    FROM public.""Providers""
                    WHERE ""Id"" = {providerId}
                    FOR UPDATE")
                    .SingleOrDefaultAsync(ct);

                if (provider is null || !provider.IsEnabled)
                    throw new InvalidOperationException($"Provider '{providerId}' not found or disabled");

                var settings = DeserializeSettings(provider.SettingsJson);
                settings.Token = newToken;

                provider.UpdateSettings(JsonSerializer.Serialize(settings, JsonOptions));

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                PutToCache(providerId, newToken, expiresAtUtc: null);
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

    // ---------------- helpers ----------------

    private static string GetCacheKey(string providerId) => $"prov:token:{providerId}";

    private void PutToCache(string providerId, string token, DateTimeOffset? expiresAtUtc)
    {
        var now = DateTimeOffset.UtcNow;

        // Если expiresAt неизвестен — кладём на короткое время, чтобы не забить кэш навсегда
        var absoluteExpiration = expiresAtUtc.HasValue
            ? expiresAtUtc.Value.UtcDateTime
            : now.AddMinutes(2).UtcDateTime;

        _cache.Set(GetCacheKey(providerId),
            new TokenCacheEntry(token, expiresAtUtc),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = absoluteExpiration
            });
    }

    private static bool IsTokenValid(string? token, DateTimeOffset? expiresAtUtc, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (!expiresAtUtc.HasValue)
            return true; // если провайдер не даёт expires — считаем валидным до 401

        // чуть раньше считаем истёкшим, чтобы не попасть на гонку около границы
        return expiresAtUtc.Value - ExpirySkew > nowUtc;
    }

    private static ProviderSettings DeserializeSettings(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new ProviderSettings();

        try
        {
            return JsonSerializer.Deserialize<ProviderSettings>(json, JsonOptions) ?? new ProviderSettings();
        }
        catch
        {
            // если внезапно битый json — не падаем всем сервисом
            return new ProviderSettings();
        }
    }

    /// <summary>
    /// PostgreSQL advisory lock (distributed lock) по providerId.
    /// Работает, даже если у тебя несколько инстансов сервиса.
    /// </summary>
    private static async Task AcquireAdvisoryLockAsync(AppDbContext db, string providerId, CancellationToken ct)
    {
        // hashtext(text) -> int4, приводим к bigint для pg_advisory_lock(bigint)
        await db.Database.ExecuteSqlInterpolatedAsync(
            $@"SELECT pg_advisory_lock(hashtext({providerId})::bigint);", ct);
    }

    private static async Task ReleaseAdvisoryLockAsync(AppDbContext db, string providerId, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $@"SELECT pg_advisory_unlock(hashtext({providerId})::bigint);", ct);
    }

    private sealed record TokenCacheEntry(string AccessToken, DateTimeOffset? ExpiresAtUtc);
}