using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;

namespace MyCompany.Transfers.Infrastructure.Workers;

internal sealed class TransferToABSOtherSenderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TransferToABSOtherSenderWorker(IServiceScopeFactory scopeFactory)
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
                var terminalRepository = scope.ServiceProvider.GetRequiredService<ITerminalRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var succeeded = await outboxRepository.GetOtherSucceededAsync();
                var ind = 0;
                var dict = new Dictionary<int, List<ProviderRequest>>();

                var list = new List<ProviderRequest>();
                foreach (var transfer in succeeded)
                {
                    var destination = await providerRepo.GetAsync(transfer.ProviderId, ct);
                    if (destination is null)
                        continue;

                    var provider = await providerRepo.GetAsync("IBT", ct);
                    if (provider is null)
                        continue;

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
                    var terminal = await terminalRepository.GetAsync(transfer.TerminalId, ct);

                    if (operation is null)
                    {
                        providerResult = new ProviderResult(
                            OutboxStatus.SETTING,
                            new Dictionary<string, string>(),
                            $"Operation '{operation}' not configured for provider '{provider.Id}'");
                    }
                    else if (terminal is null)
                    {
                        providerResult = new ProviderResult(
                            OutboxStatus.SETTING,
                            new Dictionary<string, string>(),
                            $"Terminal '{transfer.TerminalId}' not found");
                    }
                    else
                    {
                        var providerRequest = new ProviderRequest(
                            Source: transfer.Source,
                            SourceAccount: terminal.Account,
                            SourceBankIncomeAccount: terminal.BankIncomeAccount,
                            SourceCurrency: terminal.Currency,
                            Destination: destination.Name,
                            DestinationAccount: provider.Account,
                            DestinationCommissionAccount: provider.CommissionAccount,
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

                        list.Add(providerRequest);
                        if (list.Count == 100)
                        {
                            dict[ind] = list;
                            list.Clear();
                            ind++;
                        }
                    }
                }

                //foreach (var collection in dict.Values)
                //{
                //    await unitOfWork.ExecuteTransactionalAsync(async _ =>
                //    {
                //        var providerResult = await providerService.SendAsync(
                //            "IBT",
                //            collection,
                //            ct);

                //        var nowUtc = DateTimeOffset.UtcNow;

                //        if (providerResult.Status == OutboxStatus.SENT_TO_ABS)
                //        {
                //            transfer.ApplySentToAbsResult(status: OutboxStatus.SENT_TO_ABS);
                //        }

                //        return true;
                //    }, ct);
                //}

                await Task.Delay(TimeSpan.FromMinutes(30), ct);
            }
            catch
            {
                // В случае ошибки даём паузу и пробуем снова
                await Task.Delay(TimeSpan.FromMinutes(5), ct);
            }
        }
    }
}