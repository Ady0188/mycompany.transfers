using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Domain.Services;
using MyCompany.Transfers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class TransferRepository : ITransferRepository
{
    private readonly AppDbContext _db;

    public TransferRepository(AppDbContext db) => _db = db;

    public Task<Transfer?> FindByExternalIdAsync(string agentId, string externalId, CancellationToken ct) =>
        _db.Transfers.AsTracking().FirstOrDefaultAsync(x => x.AgentId == agentId && x.ExternalId == externalId, ct);

    public void Add(Transfer t) => _db.Transfers.Add(t);
    public void Update(Transfer t) => _db.Transfers.Update(t);

    public Task<Transfer?> GetStatusByExternalIdAsync(string agentId, string externalId, CancellationToken ct) =>
        _db.Transfers.AsNoTracking().FirstOrDefaultAsync(x => x.AgentId == agentId && x.ExternalId == externalId, ct);

    public Task<Transfer?> GetStatusByIdAsync(string agentId, Guid transferId, CancellationToken ct) =>
        _db.Transfers.AsNoTracking().FirstOrDefaultAsync(x => x.AgentId == agentId && x.Id == transferId, ct);

    public Task<Transfer?> GetByIdAsync(Guid transferId, CancellationToken ct) =>
        _db.Transfers.FirstOrDefaultAsync(x => x.Id == transferId, ct);

    public async Task<(IReadOnlyList<Transfer> Items, int TotalCount)> GetPagedForAdminAsync(
        int page,
        int pageSize,
        Guid? id,
        string? agentId,
        string? externalId,
        string? providerId,
        string? serviceId,
        string? status,
        DateTimeOffset? createdFrom,
        DateTimeOffset? createdTo,
        string? account,
        CancellationToken ct)
    {
        var query = _db.Transfers.AsNoTracking();

        if (id.HasValue)
            query = query.Where(t => t.Id == id.Value);
        if (!string.IsNullOrWhiteSpace(agentId))
            query = query.Where(t => t.AgentId == agentId.Trim());
        if (!string.IsNullOrWhiteSpace(externalId))
            query = query.Where(t => t.ExternalId.Contains(externalId.Trim()));
        if (!string.IsNullOrWhiteSpace(serviceId))
            query = query.Where(t => t.ServiceId == serviceId.Trim());
        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim();
            query = query.Where(t => t.Status.ToString() == st);
        }
        if (!string.IsNullOrWhiteSpace(account))
            query = query.Where(t => t.Account.Contains(account.Trim()));
        if (createdFrom.HasValue)
            query = query.Where(t => t.CreatedAtUtc >= createdFrom.Value);
        if (createdTo.HasValue)
            query = query.Where(t => t.CreatedAtUtc <= createdTo.Value);
        if (!string.IsNullOrWhiteSpace(providerId))
        {
            var pId = providerId.Trim();
            var serviceIds = await _db.Services.Where(s => s.ProviderId == pId).Select(s => s.Id).ToListAsync(ct);
            query = query.Where(t => serviceIds.Contains(t.ServiceId));
        }

        var total = await query.CountAsync(ct);
        var pageIndex = Math.Max(0, page - 1);
        var size = pageSize <= 0 ? 10 : Math.Min(pageSize, 100);
        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip(pageIndex * size)
            .Take(size)
            .ToListAsync(ct);
        return (items, total);
    }
}
