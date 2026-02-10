using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class ProviderRepository : IProviderRepository
{
    private readonly AppDbContext _db;
    public ProviderRepository(AppDbContext db) => _db = db;

    public Task<Provider?> GetAsync(string providerId, CancellationToken ct) =>
        _db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == providerId, ct);

    public Task<bool> ExistsAsync(string providerId, CancellationToken ct) =>
        _db.Providers.AsNoTracking().AnyAsync(p => p.Id == providerId, ct);

    public Task<bool> ExistsEnabledAsync(string id, CancellationToken ct) =>
        _db.Providers.AsNoTracking().AnyAsync(p => p.Id == id && p.IsEnabled, ct);
}