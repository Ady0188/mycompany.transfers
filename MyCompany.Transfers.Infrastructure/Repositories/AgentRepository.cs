using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Transfers.Dtos;
using MyCompany.Transfers.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class AgentRepository : IAgentRepository, IAgentReadRepository
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;
    public AgentRepository(AppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public Task<bool> ExistsAsync(string agentId, CancellationToken ct) =>
        _db.Agents.AnyAsync(a => a.Id == agentId, ct);

    public Task<Agent?> GetByIdAsync(string agentId, CancellationToken ct) =>
        _db.Agents.FirstOrDefaultAsync(a => a.Id == agentId, ct);

    public Task<Agent?> GetForUpdateAsync(string agentId, CancellationToken ct) =>
        _db.Agents.AsTracking().FirstOrDefaultAsync(a => a.Id == agentId, ct);

    public async Task<Agent?> GetForUpdateSqlAsync(string agentId, CancellationToken ct)
    {
        return await _db.Agents
                    .FromSqlInterpolated($@"
                            SELECT *
                            FROM public.""Agents""
                            WHERE ""Id"" = {agentId}
                            FOR UPDATE")
                    .SingleOrDefaultAsync(ct);
    }

    public async Task<BalanceResponseDto?> GetBalancesAsync(string agentId, CancellationToken ct)
    {
        var agent = await _db.Agents.AsNoTracking().FirstOrDefaultAsync(a => a.Id == agentId, ct);
        if (agent is null) return null;

        var items = agent.Balances
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new MoneyDto { Currency = kv.Key, Amount = kv.Value })
            .ToList();

        return new BalanceResponseDto
        {
            AgentId = agent.Id,
            Balances = items
        };
    }

    public async Task<long?> GetBalanceAsync(string agentId, string currency, CancellationToken ct)
    {
        var agent = await _db.Agents.AsNoTracking().FirstOrDefaultAsync(a => a.Id == agentId, ct);
        if (agent is null) return null;
        return agent.Balances.TryGetValue(currency, out var v) ? v : 0L;
    }

    private class AgentSql
    {
        public string Id { get; private set; } = default!;
        public string Balances { get; private set; }
    }
}
