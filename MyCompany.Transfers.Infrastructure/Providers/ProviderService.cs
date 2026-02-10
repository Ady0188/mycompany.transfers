using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using NLog;

namespace MyCompany.Transfers.Infrastructure.Providers;

public sealed class ProviderService : IProviderService
{
    private Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IProviderRepository _repo;
    private readonly IProviderSender _sender;
    private readonly IEnumerable<IProviderClient> _clients;

    public ProviderService(
        IProviderRepository repo,
        IProviderSender sender,
        IEnumerable<IProviderClient> clients)
    {
        _repo = repo;
        _sender = sender;
        _clients = clients;
    }

    public async Task<bool> ExistsEnabledAsync(string providerId, CancellationToken ct)
    {
        var client = _clients.FirstOrDefault(
            c => string.Equals(c.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));

        if (client != null)
            return true;

        var provider = await _repo.GetAsync(providerId, ct);
        
        return provider is not null && provider.IsEnabled;
    }

    public async Task<ProviderResult> SendAsync(string providerId, ProviderRequest request, CancellationToken ct)
    {
        try
        {
            var provider = await _repo.GetAsync(providerId, ct);
            if (provider is null)
                return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), $"Unknown provider '{providerId}'");

            var client = _clients.FirstOrDefault(
                c => string.Equals(c.ProviderId, providerId, StringComparison.OrdinalIgnoreCase));

            if (client is not null)
                return await client.SendAsync(provider, request, ct);

            return await _sender.SendAsync(provider, request, ct);
        }
        catch (Exception ex)
        {
            _logger.Error($"{ex}");
            return new ProviderResult(OutboxStatus.TECHNICAL, new Dictionary<string, string>(), ex.Message);
        }
    }
}