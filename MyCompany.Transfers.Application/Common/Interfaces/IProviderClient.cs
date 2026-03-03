using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IProviderClient
{
    string ProviderId { get; }
    Task<ProviderResult> SendAsync(Provider p, ProviderRequest request, CancellationToken ct);
}
