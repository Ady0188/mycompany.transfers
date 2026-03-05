using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Transfers.Dtos;
using MyCompany.Transfers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class AgentRepository : IAgentRepository
{
    private readonly AppDbContext _db;

    public AgentRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(string agentId, CancellationToken ct) =>
        _db.Agents.AnyAsync(a => a.Id == agentId, ct);

    public Task<Agent?> GetByIdAsync(string agentId, CancellationToken ct) =>
        _db.Agents.AsNoTracking().FirstOrDefaultAsync(a => a.Id == agentId, ct);

    public Task<Agent?> GetForUpdateAsync(string agentId, CancellationToken ct) =>
        _db.Agents.AsTracking().FirstOrDefaultAsync(a => a.Id == agentId, ct);

    public async Task<Agent?> GetForUpdateSqlAsync(string agentId, CancellationToken ct) =>
        await _db.Agents
            .FromSqlInterpolated($@"SELECT * FROM ""Agents"" WHERE ""Id"" = {agentId} FOR UPDATE")
            .AsTracking()
            .SingleOrDefaultAsync(ct);

    public async Task<BalanceResponseDto?> GetBalancesAsync(string agentId, CancellationToken ct)
    {
        if (await _db.Agents.AsNoTracking().AnyAsync(a => a.Id == agentId, ct) == false)
            return null;
        var terminals = await _db.Terminals.AsNoTracking()
            .Where(t => t.AgentId == agentId)
            .OrderBy(t => t.Currency)
            .ToListAsync(ct);
        var items = terminals.Select(t => new MoneyDto { Currency = t.Currency, Amount = t.BalanceMinor }).ToList();
        return new BalanceResponseDto { AgentId = agentId, Balances = items };
    }

    public async Task<long?> GetBalanceAsync(string agentId, string currency, CancellationToken ct)
    {
        var terminal = await _db.Terminals.AsNoTracking()
            .FirstOrDefaultAsync(t => t.AgentId == agentId && t.Currency == currency.Trim().ToUpperInvariant(), ct);
        return terminal?.BalanceMinor ?? 0L;
    }

    public async Task<IReadOnlyList<Agent>> GetAllAsync(CancellationToken ct) =>
        await _db.Agents.AsNoTracking().OrderBy(a => a.Id).ToListAsync(ct);

    public void Add(Agent agent) => _db.Agents.Add(agent);

    public void Update(Agent agent) => _db.Agents.Update(agent);

    public void Remove(Agent agent) => _db.Agents.Remove(agent);
}
