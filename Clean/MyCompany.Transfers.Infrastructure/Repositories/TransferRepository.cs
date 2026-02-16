using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Transfers;
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
}
