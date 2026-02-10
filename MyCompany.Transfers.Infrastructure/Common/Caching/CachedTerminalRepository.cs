using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Infrastructure.Common.Caching;

public sealed class CachedTerminalRepository : ITerminalRepository
{
    private readonly ITerminalRepository _inner;
    private readonly ICacheService _cache;

    public CachedTerminalRepository(
        ITerminalRepository inner,
        ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public Task<Terminal?> GetByApiKeyAsync(string apiKey, CancellationToken ct)
        => _cache.GetOrCreateAsync(
            $"term:apikey:{apiKey}",
            _ => _inner.GetByApiKeyAsync(apiKey, ct),
            TimeSpan.FromMinutes(15),
            ct);
}