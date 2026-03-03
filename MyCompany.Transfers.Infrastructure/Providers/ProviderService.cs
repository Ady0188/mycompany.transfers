using Microsoft.Extensions.Logging;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Infrastructure.Providers;

public sealed class ProviderService : IProviderService
{
    private readonly IProviderRepository _repo;
    private readonly IProviderSender _sender;
    private readonly IEnumerable<IProviderClient> _clients;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(
        IProviderRepository repo,
        IProviderSender sender,
        IEnumerable<IProviderClient> clients,
        ILogger<ProviderService> logger)
    {
        _repo = repo;
        _sender = sender;
        _clients = clients;
        _logger = logger;
    }

    public async Task<bool> ExistsEnabledAsync(string providerId, CancellationToken ct)
    {
        var client = _clients.FirstOrDefault(c => string.Equals(c.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));
        if (client is not null)
            return true;
        return await _repo.ExistsEnabledAsync(providerId, ct);
    }

    public async Task<ProviderResult> SendAsync(string providerId, ProviderRequest request, CancellationToken ct)
    {
        try
        {
            var provider = await _repo.GetAsync(providerId, ct);
            if (provider is null)
                return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), $"Unknown provider '{providerId}'");

            var client = _clients.FirstOrDefault(c => string.Equals(c.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));
            if (client is not null)
                return await client.SendAsync(provider, request, ct);

            return await _sender.SendAsync(provider, request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProviderService SendAsync failed for {ProviderId}", providerId);
            return new ProviderResult(OutboxStatus.TECHNICAL, new Dictionary<string, string>(), ex.Message);
        }
    }
}
