using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Infrastructure.Common.Caching;

public sealed class CachedAgentReadRepository : IAgentReadRepository
{
    private readonly IAgentReadRepository _inner;
    private readonly ICacheService _cache;

    public CachedAgentReadRepository(IAgentReadRepository inner, ICacheService cache)
        => (_inner, _cache) = (inner, cache);

    public Task<bool> ExistsAsync(string agentId, CancellationToken ct)
        => _cache.GetOrCreateAsync(
            key: $"agent:exists:{agentId}",
            factory: _ => _inner.ExistsAsync(agentId, ct),
            ttl: TimeSpan.FromMinutes(5),
            ct: ct);

    public Task<Agent?> GetByIdAsync(string agentId, CancellationToken ct)
        => _cache.GetOrCreateAsync(
            key: $"agent:byid:{agentId}",
            factory: _ => _inner.GetByIdAsync(agentId, ct),
            ttl: TimeSpan.FromMinutes(5),
            ct: ct);

    // ❌ НЕ кешируем: это "read-for-update" (блокировка строки)
    public Task<Agent?> GetForUpdateAsync(string agentId, CancellationToken ct)
        => _inner.GetForUpdateAsync(agentId, ct);

    public Task<Agent?> GetForUpdateSqlAsync(string agentId, CancellationToken ct)
        => _inner.GetForUpdateSqlAsync(agentId, ct);

    // Обычно НЕ кешируют. Если всё же хочешь — TTL 1-2 сек максимум.
    public Task<BalanceResponseDto?> GetBalancesAsync(string agentId, CancellationToken ct)
        => _inner.GetBalancesAsync(agentId, ct);

    public Task<long?> GetBalanceAsync(string agentId, string currency, CancellationToken ct)
        => _inner.GetBalanceAsync(agentId, currency, ct);
}