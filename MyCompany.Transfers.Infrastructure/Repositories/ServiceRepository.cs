using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;
using MyCompany.Transfers.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class ServiceRepository : IServiceRepository
{
    private readonly AppDbContext _db;
    public ServiceRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(string serviceId, CancellationToken ct) =>
        _db.Services.AnyAsync(s => s.Id == serviceId, ct);

    public Task<Service?> GetByIdAsync(string serviceId, CancellationToken ct) =>
        _db.Services.Include(s => s.Parameters).FirstOrDefaultAsync(s => s.Id == serviceId, ct);

    public async Task<(Service? Service, bool IsByPan)> GetByIdWithTypeAsync(string serviceId, CancellationToken ct)
    {
        var service = await _db.Services.Include(s => s.Parameters).FirstOrDefaultAsync(s => s.Id == serviceId, ct);

        if (service is not null)
        {
            var accountDef = await _db.AccountDefinitions
                .FirstOrDefaultAsync(ad => ad.Id == service!.AccountDefinitionId, ct);

            return (service, accountDef?.Code.Equals("PAN") ?? false);
        }

        return (service, false);
    }
}
