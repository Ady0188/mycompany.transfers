using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IOutboxReadRepository
{
    Task<List<Outbox>> GetPendingsAsync();
}

public interface IOutboxRepository : IOutboxReadRepository
{
    void Add(Outbox outbox);
    void Update(Outbox outbox);
}
