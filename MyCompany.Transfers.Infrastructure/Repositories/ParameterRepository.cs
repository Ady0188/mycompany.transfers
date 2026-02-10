using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;
using MyCompany.Transfers.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;
public sealed class ParameterRepository : IParameterRepository
{
    private readonly AppDbContext _db;
    public ParameterRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ParamDefinition>> GetAllAsync(CancellationToken ct)
        => await _db.Parameters.AsNoTracking()
            .Where(p => p.Active)
            .OrderBy(p => p.Code)
            .ToListAsync(ct);

    public async Task<ParamDefinition?> GetByIdAsync(string id, CancellationToken ct)
        => await _db.Parameters.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id && p.Active, ct);

    public async Task<ParamDefinition?> GetByCodeAsync(string code, CancellationToken ct)
        => await _db.Parameters.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Code == code && p.Active, ct);

    public async Task<Dictionary<string, ParamDefinition>> GetByIdsAsMapAsync(IEnumerable<string> ids, CancellationToken ct)
    {
        var set = ids.Distinct().ToArray();
        if (set.Length == 0) return new(StringComparer.OrdinalIgnoreCase);

        var rows = await _db.Parameters.AsNoTracking()
            .Where(p => set.Contains(p.Id) && p.Active)
            .ToListAsync(ct);

        return rows.ToDictionary(p => p.Id, p => p, StringComparer.OrdinalIgnoreCase);
    }
}