using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IServiceRepository
{
    Task<bool> ExistsAsync(string serviceId, CancellationToken ct);
    Task<Service?> GetByIdAsync(string serviceId, CancellationToken ct);
    Task<(Service? Service, bool IsByPan)> GetByIdWithTypeAsync(string serviceId, CancellationToken ct);
}
