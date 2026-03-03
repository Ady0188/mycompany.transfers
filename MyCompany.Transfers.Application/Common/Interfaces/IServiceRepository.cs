using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IServiceRepository
{
    Task<bool> ExistsAsync(string serviceId, CancellationToken ct);
    Task<Service?> GetByIdAsync(string serviceId, CancellationToken ct);
    Task<(Service? Service, bool IsByPan)> GetByIdWithTypeAsync(string serviceId, CancellationToken ct);
    Task<Service?> GetForUpdateAsync(string serviceId, CancellationToken ct);
    Task<bool> AnyByProviderIdAsync(string providerId, CancellationToken ct);
    Task<bool> AnyByAccountDefinitionIdAsync(Guid accountDefinitionId, CancellationToken ct);
    Task<IReadOnlyList<Service>> GetAllAsync(CancellationToken ct);
    void Add(Service service);
    void Update(Service service);
    void Remove(Service service);
}
