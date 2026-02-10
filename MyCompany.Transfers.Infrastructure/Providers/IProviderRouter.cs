using MyCompany.Transfers.Application.Common.Providers;

namespace MyCompany.Transfers.Infrastructure.Providers;

public interface IProviderRouter
{
    Task<bool> ExistsEnabledAsync(string providerId, CancellationToken ct);
    Task<ProviderResult> SendAsync(string providerId, ProviderRequest request, CancellationToken ct);
}
