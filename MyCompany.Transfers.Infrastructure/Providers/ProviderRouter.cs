using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Infrastructure.Providers;
internal sealed class ProviderRouter : IProviderRouter
{
    private readonly IEnumerable<IProviderClient> _clients;
    private readonly IProviderRepository _repo;

    public ProviderRouter(IEnumerable<IProviderClient> clients, IProviderRepository repo)
    {
        _clients = clients;
        _repo = repo;
    }

    public Task<bool> ExistsEnabledAsync(string providerId, CancellationToken ct)
    {
        var client = _clients.FirstOrDefault(c => string.Equals(c.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(client is not null);
    }

    public async Task<ProviderResult> SendAsync(string providerId, ProviderRequest request, CancellationToken ct)
    {
        var client = _clients.FirstOrDefault(c => string.Equals(c.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));
        if (client is null) return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), $"Unknown provider '{providerId}'");

        var provider = await _repo.GetAsync(providerId, ct);
        if (provider is null || !provider.IsEnabled)
        {
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), $"Provider '{providerId}' is not enabled");
        }

        return await client.SendAsync(provider, request, ct);
    }
}