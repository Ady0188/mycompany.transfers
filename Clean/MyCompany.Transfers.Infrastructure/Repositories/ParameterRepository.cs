using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;
using MyCompany.Transfers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class ParameterRepository : IParameterRepository
{
    private readonly AppDbContext _db;

    public ParameterRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ParamDefinition>> GetAllAsync(CancellationToken ct) =>
        await _db.Parameters.AsNoTracking().Where(p => p.Active).OrderBy(p => p.Code).ToListAsync(ct);

    public Task<IReadOnlyList<ParamDefinition>> GetAllForAdminAsync(CancellationToken ct) =>
        _db.Parameters.AsNoTracking().OrderBy(p => p.Code).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<ParamDefinition>)t.Result, ct);

    public Task<ParamDefinition?> GetByIdAsync(string id, CancellationToken ct) =>
        _db.Parameters.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id && p.Active, ct);

    public Task<ParamDefinition?> GetForUpdateAsync(string id, CancellationToken ct) =>
        _db.Parameters.SingleOrDefaultAsync(p => p.Id == id, ct);

    public Task<ParamDefinition?> GetByCodeAsync(string code, CancellationToken ct) =>
        _db.Parameters.AsNoTracking().SingleOrDefaultAsync(p => p.Code == code && p.Active, ct);

    public Task<bool> ExistsAsync(string id, CancellationToken ct) =>
        _db.Parameters.AnyAsync(p => p.Id == id, ct);

    public async Task<string> GetNextNumericIdAsync(CancellationToken ct)
    {
        var ids = await _db.Parameters.AsNoTracking().Select(p => p.Id).ToListAsync(ct);
        var next = 100;
        foreach (var id in ids)
            if (int.TryParse(id, out var num) && num >= 100)
                next = Math.Max(next, num + 1);
        return next.ToString();
    }

    public Task<bool> AnyUsedByServiceAsync(string parameterId, CancellationToken ct) =>
        _db.ServiceParamDefinitions.AnyAsync(x => x.ParameterId == parameterId, ct);

    public void Add(ParamDefinition param) => _db.Parameters.Add(param);
    public void Update(ParamDefinition param) => _db.Parameters.Update(param);
    public void Remove(ParamDefinition param) => _db.Parameters.Remove(param);

    public async Task<Dictionary<string, ParamDefinition>> GetByIdsAsMapAsync(IEnumerable<string> ids, CancellationToken ct)
    {
        var set = ids.Distinct().ToArray();
        if (set.Length == 0) return new Dictionary<string, ParamDefinition>(StringComparer.OrdinalIgnoreCase);
        var rows = await _db.Parameters.AsNoTracking().Where(p => set.Contains(p.Id) && p.Active).ToListAsync(ct);
        return rows.ToDictionary(p => p.Id, p => p, StringComparer.OrdinalIgnoreCase);
    }
}
