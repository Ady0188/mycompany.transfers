using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class TerminalRepository : ITerminalRepository
{
    private readonly AppDbContext _db;
    public TerminalRepository(AppDbContext db) => _db = db;

    public Task<Terminal?> GetByApiKeyAsync(string apiKey, CancellationToken ct) =>
        _db.Terminals.FirstOrDefaultAsync(t => t.ApiKey == apiKey && t.Active, ct);
}