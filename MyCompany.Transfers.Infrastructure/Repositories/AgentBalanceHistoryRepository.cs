using Microsoft.EntityFrameworkCore;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Infrastructure.Persistence;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class AgentBalanceHistoryRepository : IAgentBalanceHistoryRepository
{
    private readonly AppDbContext _db;

    public AgentBalanceHistoryRepository(AppDbContext db) => _db = db;

    public async Task<AgentBalanceHistory?> GetByDocIdAsync(string terminalId, long docId, CancellationToken ct)
    {
        return await _db.AgentBalanceHistories
            .AsNoTracking()
            .Where(x =>
                x.TerminalId == terminalId &&
                x.ReferenceType == BalanceHistoryReferenceType.AbsDocument &&
                x.ReferenceId == docId.ToString())
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> ExistsByReferenceAsync(string terminalId, BalanceHistoryReferenceType referenceType, string referenceId, CancellationToken ct)
    {
        return await _db.AgentBalanceHistories
            .AnyAsync(x =>
                x.TerminalId == terminalId &&
                x.ReferenceType == referenceType &&
                x.ReferenceId == referenceId, ct);
    }

    public void Add(AgentBalanceHistory history) => _db.AgentBalanceHistories.Add(history);
}

