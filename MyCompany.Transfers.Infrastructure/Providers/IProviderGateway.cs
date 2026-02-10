using MyCompany.Transfers.Application.Common.Providers;

namespace MyCompany.Transfers.Infrastructure.Providers;

public interface IProviderGateway
{
    Task<ProviderResult> SendAsync(string providerId, ProviderRequest request, CancellationToken ct);
}
