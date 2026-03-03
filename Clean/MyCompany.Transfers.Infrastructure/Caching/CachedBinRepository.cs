using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Bins;

namespace MyCompany.Transfers.Infrastructure.Caching;

public sealed class CachedBinRepository : IBinRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
    private readonly IBinRepository _inner;
    private readonly ICacheService _cache;

    private const string AllKey = "bin:all";

    public CachedBinRepository(IBinRepository inner, ICacheService cache) =>
        (_inner, _cache) = (inner, cache);

    public async Task<IReadOnlyList<Bin>> GetAllForAdminAsync(CancellationToken ct) =>
        (await _cache.GetOrCreateAsync(AllKey, async _ => (IReadOnlyList<Bin>?)await _inner.GetAllForAdminAsync(ct), Ttl, ct))
        ?? Array.Empty<Bin>();

    public Task<Bin?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"bin:by-id:{id}", _ => _inner.GetByIdAsync(id, ct), Ttl, ct);

    public Task<Bin?> GetForUpdateAsync(Guid id, CancellationToken ct) =>
        _inner.GetForUpdateAsync(id, ct);

    public Task<IReadOnlyList<Bin>> GetByCodeAsync(string code, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"bin:by-code:{code}", _ => _inner.GetByCodeAsync(code, ct), Ttl, ct);

    public Task<bool> ExistsByPrefixAsync(string prefix, Guid? excludeId, CancellationToken ct) =>
        _inner.ExistsByPrefixAsync(prefix, excludeId, ct);

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludeId, CancellationToken ct) =>
        _inner.ExistsByCodeAsync(code, excludeId, ct);

    public void Add(Bin bin)
    {
        _inner.Add(bin);
        var id = bin.Id;
        var code = bin.Code;
        _ = _cache.RemoveAsync(AllKey, default);
        _ = _cache.RemoveAsync($"bin:by-id:{id}", default);
        _ = _cache.RemoveAsync($"bin:by-code:{code}", default);
    }

    public void Update(Bin bin)
    {
        _inner.Update(bin);
        var id = bin.Id;
        var code = bin.Code;
        _ = _cache.RemoveAsync(AllKey, default);
        _ = _cache.RemoveAsync($"bin:by-id:{id}", default);
        _ = _cache.RemoveAsync($"bin:by-code:{code}", default);
    }

    public void Remove(Bin bin)
    {
        var id = bin.Id;
        var code = bin.Code;
        _inner.Remove(bin);
        _ = _cache.RemoveAsync(AllKey, default);
        _ = _cache.RemoveAsync($"bin:by-id:{id}", default);
        _ = _cache.RemoveAsync($"bin:by-code:{code}", default);
    }
}

