using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Accounts;

namespace MyCompany.Transfers.Infrastructure.Caching;

public sealed class CachedAccountDefinitionRepository : IAccountDefinitionRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(120);
    private readonly IAccountDefinitionRepository _inner;
    private readonly ICacheService _cache;

    public CachedAccountDefinitionRepository(IAccountDefinitionRepository inner, ICacheService cache) =>
        (_inner, _cache) = (inner, cache);

    public Task<AccountDefinition?> GetAsync(Guid id, CancellationToken ct = default) =>
        _cache.GetOrCreateAsync($"acc-def:by-id:{id}", _ => _inner.GetAsync(id, ct), Ttl, ct);

    public async Task<IReadOnlyList<AccountDefinition>> GetAllAsync(CancellationToken ct = default) =>
        (await _cache.GetOrCreateAsync("acc-def:all", async _ => (IReadOnlyList<AccountDefinition>?)await _inner.GetAllAsync(ct), Ttl, ct)) ?? Array.Empty<AccountDefinition>();

    public Task<AccountDefinition?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _cache.GetOrCreateAsync($"acc-def:by-code:{code.Trim().ToUpperInvariant()}", _ => _inner.GetByCodeAsync(code, ct), Ttl, ct);
}
