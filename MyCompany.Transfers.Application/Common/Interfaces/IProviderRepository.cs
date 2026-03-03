using MyCompany.Transfers.Domain.Providers;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IProviderRepository
{
    Task<Provider?> GetAsync(string providerId, CancellationToken ct);
    Task<Provider?> GetForUpdateAsync(string providerId, CancellationToken ct);
    Task<bool> ExistsAsync(string providerId, CancellationToken ct);
    Task<bool> ExistsEnabledAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken ct);
    void Add(Provider provider);
    void Update(Provider provider);
    void Remove(Provider provider);
}
