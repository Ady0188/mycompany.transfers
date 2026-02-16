using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;

namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IProviderSender
{
    Task<ProviderResult> SendAsync(Provider provider, ProviderRequest request, CancellationToken ct);
}
