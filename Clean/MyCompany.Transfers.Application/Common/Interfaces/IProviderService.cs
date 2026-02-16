using MyCompany.Transfers.Application.Common.Providers;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IProviderService
{
    Task<bool> ExistsEnabledAsync(string providerId, CancellationToken ct);
    Task<ProviderResult> SendAsync(string providerId, ProviderRequest request, CancellationToken ct);
}
