using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class TerminalRepository : ITerminalRepository
{
    private readonly AppDbContext _db;

    public TerminalRepository(AppDbContext db) => _db = db;

    public Task<Terminal?> GetByApiKeyAsync(string apiKey, CancellationToken ct) =>
        _db.Terminals.FirstOrDefaultAsync(t => t.ApiKey == apiKey && t.Active, ct);

    public Task<Terminal?> GetAsync(string terminalId, CancellationToken ct) =>
        _db.Terminals.AsNoTracking().FirstOrDefaultAsync(t => t.Id == terminalId, ct);

    public Task<Terminal?> GetForUpdateAsync(string terminalId, CancellationToken ct) =>
        _db.Terminals.FirstOrDefaultAsync(t => t.Id == terminalId, ct);

    public Task<bool> ExistsAsync(string terminalId, CancellationToken ct) =>
        _db.Terminals.AnyAsync(t => t.Id == terminalId, ct);

    public Task<IReadOnlyList<Terminal>> GetAllAsync(CancellationToken ct) =>
        _db.Terminals.AsNoTracking().OrderBy(t => t.Id).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Terminal>)t.Result, ct);

    public void Add(Terminal terminal) => _db.Terminals.Add(terminal);
    public void Update(Terminal terminal) => _db.Terminals.Update(terminal);
    public void Remove(Terminal terminal) => _db.Terminals.Remove(terminal);
}
