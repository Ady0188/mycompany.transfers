using Microsoft.EntityFrameworkCore;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Infrastructure.Persistence;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class SentCredentialsEmailRepository : ISentCredentialsEmailRepository
{
    private readonly AppDbContext _db;

    public SentCredentialsEmailRepository(AppDbContext db) => _db = db;

    public void Add(SentCredentialsEmail record) => _db.SentCredentialsEmails.Add(record);

    public async Task<IReadOnlyList<SentCredentialsEmail>> GetByAgentIdAsync(string agentId, CancellationToken ct = default) =>
        await _db.SentCredentialsEmails
            .AsNoTracking()
            .Where(x => x.AgentId == agentId)
            .OrderByDescending(x => x.SentAtUtc)
            .ToListAsync(ct);
}
