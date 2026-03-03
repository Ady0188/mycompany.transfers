using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Bins;
using MyCompany.Transfers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class BinRepository : IBinRepository
{
    private readonly AppDbContext _db;

    public BinRepository(AppDbContext db) => _db = db;

    public Task<Bin?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Bins.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<Bin?> GetForUpdateAsync(Guid id, CancellationToken ct) =>
        _db.Bins.FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<IReadOnlyList<Bin>> GetByCodeAsync(string code, CancellationToken ct) =>
        _db.Bins.AsNoTracking().Where(x => x.Code == code.ToUpper()).OrderBy(b => b.Prefix).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Bin>)t.Result, ct);

    public Task<IReadOnlyList<Bin>> GetAllForAdminAsync(CancellationToken ct) =>
        _db.Bins.AsNoTracking().OrderBy(b => b.Prefix).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Bin>)t.Result, ct);

    public Task<bool> ExistsByPrefixAsync(string prefix, Guid? excludeId, CancellationToken ct)
    {
        var norm = (prefix ?? "").Trim();
        if (string.IsNullOrEmpty(norm)) return Task.FromResult(false);
        var query = _db.Bins.Where(b => b.Prefix == norm);
        if (excludeId.HasValue)
            query = query.Where(b => b.Id != excludeId.Value);
        return query.AnyAsync(ct);
    }

    public Task<bool> ExistsByCodeAsync(string code, Guid? excludeId, CancellationToken ct)
    {
        var norm = (code ?? "").Trim();
        if (string.IsNullOrEmpty(norm)) return Task.FromResult(false);
        var query = _db.Bins.Where(b => b.Code == norm);
        if (excludeId.HasValue)
            query = query.Where(b => b.Id != excludeId.Value);
        return query.AnyAsync(ct);
    }

    public void Add(Bin bin) => _db.Bins.Add(bin);
    public void Update(Bin bin) => _db.Bins.Update(bin);
    public void Remove(Bin bin) => _db.Bins.Remove(bin);
}
