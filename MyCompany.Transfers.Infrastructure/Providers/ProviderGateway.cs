using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Infrastructure.Providers;
public sealed class ProviderGateway : IProviderGateway
{
    private readonly IProviderRepository _repo;
    private readonly IProviderSender _sender;

    public ProviderGateway(IProviderRepository repo, IProviderSender sender) =>
        (_repo, _sender) = (repo, sender);

    public async Task<ProviderResult> SendAsync(string providerId, ProviderRequest request, CancellationToken ct)
    {
        var provider = await _repo.GetAsync(providerId, ct);
        if (provider is null)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), $"Provider '{providerId}' not found or disabled");

        return await _sender.SendAsync(provider, request, ct);
    }
}