using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Infrastructure.Caching;

public sealed class CachedTerminalRepository : ITerminalRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);
    private readonly ITerminalRepository _inner;
    private readonly ICacheService _cache;

    public CachedTerminalRepository(ITerminalRepository inner, ICacheService cache) =>
        (_inner, _cache) = (inner, cache);

    public Task<Terminal?> GetByApiKeyAsync(string apiKey, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"term:apikey:{apiKey}", _ => _inner.GetByApiKeyAsync(apiKey, ct), Ttl, ct);

    public Task<Terminal?> GetAsync(string terminalId, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"term:id:{terminalId}", _ => _inner.GetAsync(terminalId, ct), Ttl, ct);

    public Task<Terminal?> GetForUpdateAsync(string terminalId, CancellationToken ct) =>
        _inner.GetForUpdateAsync(terminalId, ct);

    public Task<bool> ExistsAsync(string terminalId, CancellationToken ct) =>
        _cache.GetOrCreateAsync($"term:exists:{terminalId}", _ => _inner.ExistsAsync(terminalId, ct), Ttl, ct);

    public Task<bool> AnyByAgentIdAsync(string agentId, CancellationToken ct) =>
        _inner.AnyByAgentIdAsync(agentId, ct);

    private const string AllKey = "term:all";

    public async Task<IReadOnlyList<Terminal>> GetAllAsync(CancellationToken ct) =>
        (await _cache.GetOrCreateAsync(AllKey, async _ => (IReadOnlyList<Terminal>?)await _inner.GetAllAsync(ct), Ttl, ct)) ?? Array.Empty<Terminal>();

    public void Add(Terminal terminal)
    {
        _inner.Add(terminal);
        var id = terminal.Id;
        _ = _cache.RemoveAsync(AllKey, default);
        _ = _cache.RemoveAsync($"term:id:{id}", default);
        _ = _cache.RemoveAsync($"term:exists:{id}", default);
        if (!string.IsNullOrWhiteSpace(terminal.ApiKey))
            _ = _cache.RemoveAsync($"term:apikey:{terminal.ApiKey}", default);
    }

    public void Update(Terminal terminal)
    {
        _inner.Update(terminal);
        var id = terminal.Id;
        _ = _cache.RemoveAsync(AllKey, default);
        _ = _cache.RemoveAsync($"term:id:{id}", default);
        _ = _cache.RemoveAsync($"term:exists:{id}", default);
        if (!string.IsNullOrWhiteSpace(terminal.ApiKey))
            _ = _cache.RemoveAsync($"term:apikey:{terminal.ApiKey}", default);
    }

    public void Remove(Terminal terminal)
    {
        var id = terminal.Id;
        _inner.Remove(terminal);
        _ = _cache.RemoveAsync(AllKey, default);
        _ = _cache.RemoveAsync($"term:id:{id}", default);
        _ = _cache.RemoveAsync($"term:exists:{id}", default);
        if (!string.IsNullOrWhiteSpace(terminal.ApiKey))
            _ = _cache.RemoveAsync($"term:apikey:{terminal.ApiKey}", default);
    }
}
