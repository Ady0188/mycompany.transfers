using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;

namespace MyCompany.Transfers.Infrastructure.Workers;

internal sealed class TransferToABSSenderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TransferToABSSenderWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                var providerRepo = scope.ServiceProvider.GetRequiredService<IProviderRepository>();
                var providerService = scope.ServiceProvider.GetRequiredService<IProviderService>();
                var agentRepository = scope.ServiceProvider.GetRequiredService<IAgentReadRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var succeeded = await outboxRepository.GetSucceededAsync();

                foreach (var transfer in succeeded)
                {
                    await unitOfWork.ExecuteTransactionalAsync(async _ =>
                    {
                        var destination = await providerRepo.GetAsync(transfer.ProviderId, ct);
                        if (destination is null)
                            return false;

                        var provider = await providerRepo.GetAsync("IBT", ct);
                        if (provider is null)
                            return false;

                        var settings = provider.SettingsJson.Deserialize<ProviderSettings>()
                                       ?? new ProviderSettings();

                        var providerResult = new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), null);

                        if (settings.JobScenario.Count == 0)
                        {
                            providerResult = new ProviderResult(
                                OutboxStatus.SETTING,
                                new Dictionary<string, string>(),
                                $"No job scenarios configured for provider '{provider.Id}'");
                        }

                        var operation = settings.JobScenario.TryGetValue(transfer.Status, out var op) ? op : null;
                        var agent = await agentRepository.GetByIdAsync(transfer.AgentId, ct);
                        
                        if (operation is null)
                        {
                            providerResult = new ProviderResult(
                                OutboxStatus.SETTING,
                                new Dictionary<string, string>(),
                                $"Operation '{operation}' not configured for provider '{provider.Id}'");
                        }
                        else if (agent is null)
                        {
                            providerResult = new ProviderResult(
                                OutboxStatus.SETTING,
                                new Dictionary<string, string>(),
                                $"Agent with id '{transfer.AgentId}' not found");
                        }
                        else
                        {
                            var providerRequest = new ProviderRequest(
                                Source: transfer.Source,
                                SourceAccount: agent.Account,
                                SourceCurrency: transfer.Amount.Currency,
                                Destination: destination.Name,
                                DestinationAccount: provider.Account,
                                Operation: operation,
                                TransferId: transfer.TransferId.ToString(),
                                NumId: transfer.NumId,
                                ExternalId: transfer.ExternalId,
                                ServiceId: transfer.ServiceId,
                                ProviderServiceId: transfer.ProviderServiceId,
                                Account: transfer.Account,
                                SourceAmount: transfer.Amount.Minor,
                                SourceFeeAmount: transfer.CurrentQuote!.Fee.Minor,
                                TotalAmount: transfer.CurrentQuote!.Total.Minor,
                                CreditAmount: transfer.CurrentQuote!.CreditedAmount.Minor,
                                ProviderFee: transfer.CurrentQuote!.ProviderFee.Minor,
                                CurrencyIsoCode: transfer.CurrentQuote!.CreditedAmount.Currency,
                                ExchangeRate: transfer.CurrentQuote!.ExchangeRate ?? 0,
                                Proc: "",
                                Parameters: transfer.Parameters,
                                ProvReceivedParams: transfer.ProvReceivedParams,
                                TransferDateTime: transfer.CreatedAtUtc);

                            providerResult = await providerService.SendAsync(
                                "IBT",
                                providerRequest,
                                ct);
                        }
                        
                        var nowUtc = DateTimeOffset.UtcNow;

                        if (providerResult.Status == OutboxStatus.SENT_TO_ABS)
                        {
                            transfer.ApplySentToAbsResult(status: OutboxStatus.SENT_TO_ABS);
                        }

                        return true;
                    }, ct);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
            catch
            {
                // В случае ошибки даём паузу и пробуем снова
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }
}

