using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Rates;
using MyCompany.Transfers.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class FxRateRepository : IFxRateRepository
{
    private readonly AppDbContext _db;
    public FxRateRepository(AppDbContext db) => _db = db;

    public async Task<(decimal, DateTimeOffset)?> GetAsync(string baseCcy, string quoteCcy, CancellationToken ct)
    {
        if (baseCcy.Equals(quoteCcy, StringComparison.OrdinalIgnoreCase))
            return (1m, DateTimeOffset.UtcNow);

        var row = await _db.Set<FxRate>()
            .AsNoTracking()
            .Where(r => r.BaseCurrency == baseCcy && r.QuoteCurrency == quoteCcy && r.IsActive)
            .Select(r => new { r.Rate, r.UpdatedAtUtc })
            .SingleOrDefaultAsync(ct);

        return row is null ? null : (row.Rate, row.UpdatedAtUtc);
    }
}