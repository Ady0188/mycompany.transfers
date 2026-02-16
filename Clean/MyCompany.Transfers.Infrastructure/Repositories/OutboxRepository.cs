using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MyCompany.Transfers.Infrastructure.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _db;

    public OutboxRepository(AppDbContext db) => _db = db;

    public void Add(Outbox outbox) => _db.Outboxes.Add(outbox);
    public void Update(Outbox outbox) => _db.Outboxes.Update(outbox);

    public Task<List<Outbox>> GetPendingsAsync() =>
        _db.Outboxes
            .Where(x => x.Status == OutboxStatus.TO_SEND || x.Status == OutboxStatus.SENDING || x.Status == OutboxStatus.STATUS)
            .ToListAsync();
}
