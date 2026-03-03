using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Infrastructure.Caching;

public sealed class CachedParameterRepository : IParameterRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);
    private readonly IParameterRepository _inner;
    private readonly ICacheService _cache;
    private const string AllKey = "param:all";

    public CachedParameterRepository(IParameterRepository inner, ICacheService cache) =>
        (_inner, _cache) = (inner, cache);

    public async Task<IReadOnlyList<ParamDefinition>> GetAllAsync(CancellationToken ct) =>
        (await _cache.GetOrCreateAsync(AllKey, async _ => (IReadOnlyList<ParamDefinition>?)await _inner.GetAllAsync(ct), Ttl, ct)) ?? Array.Empty<ParamDefinition>();

    public Task<ParamDefinition?> GetByIdAsync(string id, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"param:by-id:{id}", _ => _inner.GetByIdAsync(id, ct), Ttl, ct);

    public Task<ParamDefinition?> GetByCodeAsync(string code, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"param:by-code:{code}", _ => _inner.GetByCodeAsync(code, ct), Ttl, ct);

    public async Task<Dictionary<string, ParamDefinition>> GetByIdsAsMapAsync(IEnumerable<string> ids, CancellationToken ct)
    {
        var result = new Dictionary<string, ParamDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids.Distinct())
        {
            var param = await _cache.GetOrCreateAsync($"param:by-id:{id}", _ => _inner.GetByIdAsync(id, ct), Ttl, ct);
            if (param is not null)
                result[id] = param;
        }
        return result;
    }

    public Task<IReadOnlyList<ParamDefinition>> GetAllForAdminAsync(CancellationToken ct) =>
        _inner.GetAllForAdminAsync(ct);

    public Task<ParamDefinition?> GetForUpdateAsync(string id, CancellationToken ct) =>
        _inner.GetForUpdateAsync(id, ct);

    public Task<bool> ExistsAsync(string id, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"param:exists:{id}", _ => _inner.ExistsAsync(id, ct), Ttl, ct);

    public Task<string> GetNextNumericIdAsync(CancellationToken ct) =>
        _inner.GetNextNumericIdAsync(ct);

    public Task<bool> AnyUsedByServiceAsync(string parameterId, CancellationToken ct) =>
        _inner.AnyUsedByServiceAsync(parameterId, ct);

    public void Add(ParamDefinition param)
    {
        _inner.Add(param);
        var id = param.Id;
        var code = param.Code;
        _ = _cache.RemoveAsync(AllKey, default);
        _ = _cache.RemoveAsync($"param:by-id:{id}", default);
        _ = _cache.RemoveAsync($"param:by-code:{code}", default);
        _ = _cache.RemoveAsync($"param:exists:{id}", default);
    }

    public void Update(ParamDefinition param)
    {
        _inner.Update(param);
        var id = param.Id;
        var code = param.Code;
        _ = _cache.RemoveAsync(AllKey, default);
        _ = _cache.RemoveAsync($"param:by-id:{id}", default);
        _ = _cache.RemoveAsync($"param:by-code:{code}", default);
        _ = _cache.RemoveAsync($"param:exists:{id}", default);
    }

    public void Remove(ParamDefinition param)
    {
        var id = param.Id;
        var code = param.Code;
        _inner.Remove(param);
        _ = _cache.RemoveAsync(AllKey, default);
        _ = _cache.RemoveAsync($"param:by-id:{id}", default);
        _ = _cache.RemoveAsync($"param:by-code:{code}", default);
        _ = _cache.RemoveAsync($"param:exists:{id}", default);
    }
}
