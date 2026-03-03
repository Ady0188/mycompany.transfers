using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class ProviderRepository : IProviderRepository
{
    private readonly AppDbContext _db;

    public ProviderRepository(AppDbContext db) => _db = db;

    public Task<Provider?> GetAsync(string providerId, CancellationToken ct) =>
        _db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == providerId, ct);

    public Task<Provider?> GetForUpdateAsync(string providerId, CancellationToken ct) =>
        _db.Providers.FirstOrDefaultAsync(p => p.Id == providerId, ct);

    public Task<bool> ExistsAsync(string providerId, CancellationToken ct) =>
        _db.Providers.AnyAsync(p => p.Id == providerId, ct);

    public Task<bool> ExistsEnabledAsync(string id, CancellationToken ct) =>
        _db.Providers.AnyAsync(p => p.Id == id && p.IsEnabled, ct);

    public Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken ct) =>
        _db.Providers.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct).ContinueWith(t => (IReadOnlyList<Provider>)t.Result, ct);

    public void Add(Provider provider) => _db.Providers.Add(provider);

    public void Update(Provider provider) => _db.Providers.Update(provider);

    public void Remove(Provider provider) => _db.Providers.Remove(provider);
}
