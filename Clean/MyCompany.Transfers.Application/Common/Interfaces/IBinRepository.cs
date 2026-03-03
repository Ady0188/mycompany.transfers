using MyCompany.Transfers.Domain.Bins;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IBinRepository
{
    Task<Bin?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Bin?> GetForUpdateAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Bin>> GetAllForAdminAsync(CancellationToken ct);
    Task<bool> ExistsByPrefixAsync(string prefix, Guid? excludeId, CancellationToken ct);
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId, CancellationToken ct);
    void Add(Bin bin);
    void Update(Bin bin);
    void Remove(Bin bin);
}
