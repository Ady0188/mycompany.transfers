using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Infrastructure.Common.Caching;

public sealed class CachedParameterRepository : IParameterRepository
{
    private readonly IParameterRepository _inner;
    private readonly ICacheService _cache;
    private static readonly TimeSpan ttl = TimeSpan.FromMinutes(30);

    public CachedParameterRepository(IParameterRepository inner, ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<IReadOnlyList<ParamDefinition>> GetAllAsync(CancellationToken ct)
        => _cache.GetOrCreateAsync(
            $"param:all",
            _ => _inner.GetAllAsync(ct),
            ttl,
            ct);

    public Task<ParamDefinition?> GetByCodeAsync(string code, CancellationToken ct)
        => _cache.GetOrCreateAsync(
            $"param:by-code:{code}",
            _ => _inner.GetByCodeAsync(code, ct),
            ttl,
            ct);

    public Task<ParamDefinition?> GetByIdAsync(string id, CancellationToken ct)
        => _cache.GetOrCreateAsync(
            $"param:by-id:{id}",
            _ => _inner.GetByIdAsync(id, ct),
            ttl,
            ct);

    public async Task<Dictionary<string, ParamDefinition>> GetByIdsAsMapAsync(IEnumerable<string> ids, CancellationToken ct)
    {
        var result = new Dictionary<string, ParamDefinition>();

        foreach (var id in ids.Select(x => x).Distinct())
        {
            var param = await _cache.GetOrCreateAsync(
                $"param:by-id:{id}",
                _ => _inner.GetByIdAsync(id, ct),
                ttl,
                ct);

            if (param is not null)
                result[id] = param;
        }

        return result;
    }
}
