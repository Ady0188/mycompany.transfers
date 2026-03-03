using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface ITransferReadRepository
{
    Task<Transfer?> GetByIdAsync(Guid transferId, CancellationToken ct);
    Task<Transfer?> GetStatusByExternalIdAsync(string agentId, string externalId, CancellationToken ct);
    Task<Transfer?> GetStatusByIdAsync(string agentId, Guid transferId, CancellationToken ct);
    Task<Transfer?> FindByExternalIdAsync(string agentId, string externalId, CancellationToken ct);
    Task<(IReadOnlyList<Transfer> Items, int TotalCount)> GetPagedForAdminAsync(
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
        CancellationToken ct);
}

public interface ITransferRepository : ITransferReadRepository
{
    void Add(Transfer transfer);
    void Update(Transfer transfer);
}
