using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Accounts;

namespace MyCompany.Transfers.Infrastructure.Common.Caching;

public sealed class CachedAccountDefinitionRepository : IAccountDefinitionRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(120);

    private readonly IAccountDefinitionRepository _inner;
    private readonly ICacheService _cache;

    public CachedAccountDefinitionRepository(IAccountDefinitionRepository inner, ICacheService cache)
        => (_inner, _cache) = (inner, cache);

    public Task<IReadOnlyList<AccountDefinition>> GetAllAsync(CancellationToken ct = default)
        => _cache.GetOrCreateAsync(
            "acc-def:all",
            _ => _inner.GetAllAsync(ct),
            Ttl,
            ct);

    public Task<AccountDefinition?> GetAsync(Guid id, CancellationToken ct = default)
        => _cache.GetOrCreateAsync(
            $"acc-def:by-id:{id}",
            _ => _inner.GetAsync(id, ct),
            Ttl,
            ct);

    public Task<AccountDefinition?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var c = Norm(code);
        return _cache.GetOrCreateAsync(
            $"acc-def:by-code:{c}",
            _ => _inner.GetByCodeAsync(code, ct),
            Ttl,
            ct);
    }

    private static string Norm(string s) => s.Trim().ToUpperInvariant();
}
