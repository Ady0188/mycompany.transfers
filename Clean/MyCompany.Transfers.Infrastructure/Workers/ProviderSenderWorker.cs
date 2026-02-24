using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyCompany.Transfers.Infrastructure.Workers;

/// <summary>
/// Фоновый воркер, который берёт сообщения из Outbox и отправляет их в провайдеров через IProviderService.
/// Поведение перенесено из оригинального проекта.
/// </summary>
internal sealed class ProviderSenderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ProviderSenderWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                var providerRepo = scope.ServiceProvider.GetRequiredService<IProviderRepository>();
                var providerService = scope.ServiceProvider.GetRequiredService<IProviderService>();
                var transferRepository = scope.ServiceProvider.GetRequiredService<ITransferRepository>();
                var agentRepository = scope.ServiceProvider.GetRequiredService<IAgentReadRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var pending = await outboxRepository.GetPendingsAsync();

                if (pending.Count > 0)
                {
                    await ProcessBatchAsync(
                        pending,
                        providerRepo,
                        providerService,
                        transferRepository,
                        outboxRepository,
                        agentRepository,
                        unitOfWork,
                        stoppingToken);

                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                else
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch
            {
                // В случае ошибки даём паузу и пробуем снова
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private static async Task ProcessBatchAsync(
        List<Outbox> pending,
        IProviderRepository providerRepo,
        IProviderService providerService,
        ITransferRepository transferRepository,
        IOutboxRepository outboxRepository,
        IAgentReadRepository agentRepository,
        IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        const int maxDegreeOfParallelism = 10;
        using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

        var tasks = pending.Select(async outbox =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                await ProcessMessageAsync(
                    outbox,
                    providerRepo,
                    providerService,
                    transferRepository,
                    outboxRepository,
                    agentRepository,
                    unitOfWork,
                    ct);
            }
            catch
            {
                outbox.ApplyProviderResult(
                    DateTimeOffset.UtcNow,
                    outbox.ProviderCode ?? "UNKNOWN",
                    "Unexpected worker error",
                    null,
                    ProviderResultKind.Error,
                    OutboxStatus.FAILED);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private static async Task ProcessMessageAsync(
        Outbox msg,
        IProviderRepository providerRepo,
        IProviderService providerService,
        ITransferRepository transferRepository,
        IOutboxRepository outboxRepository,
        IAgentReadRepository agentRepository,
        IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        await unitOfWork.ExecuteTransactionalAsync(async _ =>
        {
            var provider = await providerRepo.GetAsync(msg.ProviderId, ct);
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

            var operation = settings.JobScenario.TryGetValue(msg.Status, out var op) ? op : null;

            if (operation is null)
            {
                providerResult = new ProviderResult(
                    OutboxStatus.SETTING,
                    new Dictionary<string, string>(),
                    $"Operation '{operation}' not configured for provider '{provider.Id}'");
            }
            else
            {
                var providerRequest = new ProviderRequest(
                    msg.Source,
                    operation,
                    msg.TransferId.ToString(),
                    msg.NumId,
                    msg.ExternalId,
                    msg.ServiceId,
                    msg.ProviderServiceId,
                    msg.Account,
                    msg.CurrentQuote!.CreditedAmount.Minor,
                    msg.CurrentQuote!.ProviderFee.Minor,
                    msg.CurrentQuote!.CreditedAmount.Currency,
                    "",
                    msg.Parameters,
                    msg.ProvReceivedParams,
                    msg.CreatedAtUtc);

                providerResult = await providerService.SendAsync(
                    msg.ProviderId,
                    providerRequest,
                    ct);
            }

            var transfer = await transferRepository.GetByIdAsync(msg.TransferId, ct);
            if (transfer is null)
                return false;

            var nowUtc = DateTimeOffset.UtcNow;

            if (providerResult.Status == OutboxStatus.SUCCESS)
            {
                msg.SetReceivedParams(providerResult.ResponseFields);
                outboxRepository.Update(msg);

                transfer.SetReceivedParams(providerResult.ResponseFields);
                transferRepository.Update(transfer);

                transfer.ApplyProviderResult(
                    nowUtc,
                    providerCode: "0",
                    description: "OK",
                    providerTransferId: null,
                    kind: ProviderResultKind.Success,
                    status: TransferStatus.SUCCESS);

                msg.ApplyProviderResult(
                    nowUtc,
                    providerCode: "0",
                    description: "OK",
                    providerTransferId: null,
                    kind: ProviderResultKind.Success,
                    status: OutboxStatus.SUCCESS);
            }
            else if (providerResult.Status == OutboxStatus.SENDING || providerResult.Status == OutboxStatus.STATUS)
            {
                msg.SetReceivedParams(providerResult.ResponseFields);
                outboxRepository.Update(msg);

                transfer.SetReceivedParams(providerResult.ResponseFields);
                transferRepository.Update(transfer);

                msg.ApplyProviderResult(
                    nowUtc,
                    providerCode: "0",
                    description: "OK",
                    providerTransferId: null,
                    kind: ProviderResultKind.Success,
                    status: providerResult.Status);
            }
            else
            {
                var prvCode = providerResult.ResponseFields.TryGetValue("errorCode", out var c) ? c : "UNKNOWN";

                msg.ApplyProviderResult(
                    nowUtc,
                    providerCode: prvCode,
                    description: providerResult.Error ?? "Provider error",
                    providerTransferId: null,
                    kind: ProviderResultKind.Technical,
                    status: providerResult.Status);
            }

            if (providerResult.Status == OutboxStatus.FAILED ||
                providerResult.Status == OutboxStatus.FRAUD ||
                providerResult.Status == OutboxStatus.EXPIRED)
            {
                var prvCode = providerResult.ResponseFields.TryGetValue("errorCode", out var c) ? c : "UNKNOWN";

                var trnSts = providerResult.Status switch
                {
                    OutboxStatus.FAILED => TransferStatus.FAILED,
                    OutboxStatus.FRAUD => TransferStatus.FRAUD,
                    OutboxStatus.EXPIRED => TransferStatus.EXPIRED,
                    _ => TransferStatus.FAILED
                };

                transfer.ApplyProviderResult(
                    nowUtc,
                    providerCode: prvCode,
                    description: providerResult.Error ?? "Provider error",
                    providerTransferId: null,
                    kind: ProviderResultKind.Error,
                    status: trnSts);

                var agent = await agentRepository.GetForUpdateSqlAsync(transfer.AgentId, ct);
                agent?.Credit(transfer.CurrentQuote!.Total.Currency, transfer.CurrentQuote.Total.Minor);
            }

            return true;
        }, ct);
    }
}

