using Microsoft.EntityFrameworkCore;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Infrastructure.Persistence;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class AgentBalanceHistoryRepository : IAgentBalanceHistoryRepository
{
    private readonly AppDbContext _db;

    public AgentBalanceHistoryRepository(AppDbContext db) => _db = db;

    public async Task<AgentBalanceHistory?> GetByDocIdAsync(string agentId, string currency, long docId, CancellationToken ct)
    {
        return await _db.AgentBalanceHistories
            .AsNoTracking()
            .Where(x =>
                x.AgentId == agentId &&
                x.Currency == currency &&
                x.ReferenceType == BalanceHistoryReferenceType.AbsDocument &&
                x.ReferenceId == docId.ToString())
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> ExistsByReferenceAsync(string agentId, string currency, BalanceHistoryReferenceType referenceType, string referenceId, CancellationToken ct)
    {
        return await _db.AgentBalanceHistories
            .AnyAsync(x =>
                x.AgentId == agentId &&
                x.Currency == currency &&
                x.ReferenceType == referenceType &&
                x.ReferenceId == referenceId, ct);
    }

    public void Add(AgentBalanceHistory history) => _db.AgentBalanceHistories.Add(history);
}

