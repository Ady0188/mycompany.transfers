using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Infrastructure.Caching;

public sealed class CachedAgentReadRepository : IAgentReadRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private readonly IAgentReadRepository _inner;
    private readonly ICacheService _cache;

    public CachedAgentReadRepository(IAgentReadRepository inner, ICacheService cache) =>
        (_inner, _cache) = (inner, cache);

    public Task<bool> ExistsAsync(string agentId, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"agent:exists:{agentId}", _ => _inner.ExistsAsync(agentId, ct), Ttl, ct);

    public Task<Agent?> GetByIdAsync(string agentId, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"agent:byid:{agentId}", _ => _inner.GetByIdAsync(agentId, ct), Ttl, ct);

    public Task<Agent?> GetForUpdateAsync(string agentId, CancellationToken ct) =>
        _inner.GetForUpdateAsync(agentId, ct);

    public Task<Agent?> GetForUpdateSqlAsync(string agentId, CancellationToken ct) =>
        _inner.GetForUpdateSqlAsync(agentId, ct);

    public Task<BalanceResponseDto?> GetBalancesAsync(string agentId, CancellationToken ct) =>
        _inner.GetBalancesAsync(agentId, ct);

    public Task<long?> GetBalanceAsync(string agentId, string currency, CancellationToken ct) =>
        _inner.GetBalanceAsync(agentId, currency, ct);
}
