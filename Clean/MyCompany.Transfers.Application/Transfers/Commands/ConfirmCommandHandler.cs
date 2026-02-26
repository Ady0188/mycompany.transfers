using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Services;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Transfers.Commands;

public sealed class ConfirmCommandHandler : IRequestHandler<ConfirmCommand, ErrorOr<ConfirmResponseDto>>
{
    private readonly ITransferRepository _transfers;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IServiceRepository _services;
    private readonly IProviderRepository _providers;
    private readonly IProviderService _providerService;
    private readonly IAgentReadRepository _agentRepository;
    private readonly IAgentBalanceHistoryRepository _balanceHistory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    private readonly ILogger<ConfirmCommandHandler> _logger;

    public ConfirmCommandHandler(
        ITransferRepository transfers,
        IServiceRepository services,
        IUnitOfWork unitOfWork,
        IProviderRepository providers,
        TimeProvider clock,
        IOutboxRepository outboxRepository,
        IProviderService providerService,
        IAgentReadRepository agentRepository,
        IAgentBalanceHistoryRepository balanceHistory,
        ILogger<ConfirmCommandHandler> logger)
    {
        _transfers = transfers;
        _services = services;
        _unitOfWork = unitOfWork;
        _providers = providers;
        _clock = clock;
        _outboxRepository = outboxRepository;
        _providerService = providerService;
        _agentRepository = agentRepository;
        _balanceHistory = balanceHistory;
        _logger = logger;
    }

    public async Task<ErrorOr<ConfirmResponseDto>> Handle(ConfirmCommand m, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("ConfirmCommandHandler: AgentId={AgentId}, ExternalId={ExternalId}, QuotationId={QuotationId}", m.AgentId, m.ExternalId, m.QuotationId);

            ConfirmResponseDto? response = null;
            ProviderRequest? providerReq = null;
            Transfer? transfer = null;
            string providerId = string.Empty;
            bool isProviderOnline = false;
            Outbox? outbox = null;
            Error? error = null;

            await _unitOfWork.ExecuteTransactionalAsync(async _ =>
            {
                transfer = await _transfers.FindByExternalIdAsync(m.AgentId, m.ExternalId, ct);
                if (transfer is null)
                {
                    _logger.LogInformation("ConfirmCommandHandler: transfer not found ExternalId={ExternalId}", m.ExternalId);
                    error = AppErrors.Transfers.NotFound(m.ExternalId);
                    return false;
                }

                var agent = await _agentRepository.GetForUpdateAsync(m.AgentId, ct);

                switch (transfer.Status)
                {
                    case TransferStatus.PREPARED:
                        break;
                    case TransferStatus.CONFIRMED:
                        _logger.LogInformation("ConfirmCommandHandler: transfer already confirmed ExternalId={ExternalId}, Id={Id}", m.ExternalId, transfer.Id);
                        response = transfer.ToConfirmResponseDto(agent!);
                        return true;
                    case TransferStatus.SUCCESS:
                    case TransferStatus.FAILED:
                    case TransferStatus.EXPIRED:
                    case TransferStatus.FRAUD:
                        _logger.LogInformation("ConfirmCommandHandler: transfer already finished ExternalId={ExternalId}, Id={Id}", m.ExternalId, transfer.Id);
                        error = AppErrors.Transfers.AlreadyFinished(m.ExternalId, transfer.Status.ToString());
                        return false;
                    default:
                        _logger.LogInformation("ConfirmCommandHandler: transfer invalid status for confirm ExternalId={ExternalId}, Status={Status}", m.ExternalId, transfer.Status);
                        error = AppErrors.Transfers.NotPrepared(transfer.Status.ToString());
                        return false;
                }

                if (transfer.CurrentQuote is null || transfer.CurrentQuote.Id != m.QuotationId)
                {
                    _logger.LogInformation("ConfirmCommandHandler: quote mismatch ExternalId={ExternalId}, QuotationId={QuotationId}", m.ExternalId, m.QuotationId);
                    error = AppErrors.Transfers.QuoteMismatch;
                    return false;
                }

                var nowUtc = _clock.GetUtcNow();
                if (transfer.CurrentQuote.IsExpired(nowUtc))
                {
                    _logger.LogInformation("ConfirmCommandHandler: quote expired ExternalId={ExternalId}, QuotationId={QuotationId}", m.ExternalId, m.QuotationId);
                    error = AppErrors.Transfers.QuoteExpired;
                    return false;
                }

                var service = await _services.GetByIdAsync(transfer.ServiceId, ct);
                if (service is null)
                {
                    _logger.LogInformation("ConfirmCommandHandler: service not found ServiceId={ServiceId}", transfer.ServiceId);
                    error = AppErrors.Transfers.InvalidRequest($"Услуга '{transfer.ServiceId}' не найдена.");
                    return false;
                }

                providerId = service.ProviderId;
                var provider = await _providers.GetAsync(providerId, ct);
                if (provider is null || !provider.IsEnabled)
                {
                    _logger.LogWarning("ConfirmCommandHandler: provider not found or disabled ProviderId={ProviderId}", service.ProviderId);
                    error = AppErrors.Common.Validation($"Провайдер '{service.ProviderId}' не найден или отключён.");
                    return false;
                }

                isProviderOnline = provider.IsOnline;

                agent = await _agentRepository.GetForUpdateSqlAsync(m.AgentId, ct);
                if (agent is null)
                {
                    _logger.LogInformation("ConfirmCommandHandler: agent not found AgentId={AgentId}", m.AgentId);
                    error = AppErrors.Agents.NotFound(m.AgentId);
                    return false;
                }

                var total = transfer.CurrentQuote.Total;
                if (!agent.HasSufficientBalance(total.Currency, total.Minor))
                {
                    _logger.LogInformation("ConfirmCommandHandler: insufficient balance Currency={Currency}, RequiredMinor={Minor}", total.Currency, total.Minor);
                    error = AppErrors.Agents.InsufficientBalance(total.Currency);
                    return false;
                }

                var debitRefId = $"{transfer.Id}:Debit";
                var alreadyDebited = await _balanceHistory.ExistsByReferenceAsync(agent.Id, total.Currency, BalanceHistoryReferenceType.Transfer, debitRefId, ct);
                if (!alreadyDebited)
                {
                    var currentBalance = agent.Balances.TryGetValue(total.Currency, out var cur) ? cur : 0L;
                    agent.Debit(total.Currency, total.Minor);
                    var newBalance = agent.Balances.TryGetValue(total.Currency, out var upd) ? upd : currentBalance - total.Minor;
                    var history = AgentBalanceHistory.CreateForTransfer(
                        agent.Id,
                        debitRefId,
                        nowUtc.UtcDateTime,
                        total.Currency,
                        currentBalance,
                        -total.Minor,
                        newBalance);
                    _balanceHistory.Add(history);
                }

                transfer.MarkConfirmed(nowUtc);
                response = transfer.ToConfirmResponseDto(agent);

                providerReq = new ProviderRequest(
                    agent.Id, "confirm", transfer.Id.ToString(), transfer.NumId, transfer.ExternalId,
                    service.Id, service.ProviderServiceId, transfer.Account,
                    transfer.CurrentQuote!.CreditedAmount.Minor, transfer.CurrentQuote!.ProviderFee.Minor,
                    service.AllowedCurrencies.First(), service.Name, transfer.Parameters, transfer.ProvReceivedParams, transfer.CreatedAtUtc);

                outbox = Outbox.Create(transfer, service, agent.Id);
                return true;
            }, ct);

            if (error is not null)
            {
                _logger.LogInformation("ConfirmCommandHandler: returning error");
                return error.Value;
            }

            if (isProviderOnline)
            {
                var providerResult = await _providerService.SendAsync(providerId, providerReq!, ct);

                if (providerResult.Status == OutboxStatus.SUCCESS)
                {
                    transfer!.MarkCompleted(_clock.GetUtcNow(), TransferStatus.SUCCESS);
                    transfer.ApplyProviderResult(
                        _clock.GetUtcNow(), "0", "OK", null, ProviderResultKind.Success, TransferStatus.SUCCESS);
                }
                else if (providerResult.Status == OutboxStatus.TECHNICAL || providerResult.Status == OutboxStatus.FAILED)
                {
                    var prvCode = providerResult.ResponseFields.TryGetValue("errorCode", out var c) ? c : "UNKNOWN";
                    var status = providerResult.Status == OutboxStatus.TECHNICAL ? TransferStatus.TECHNICAL : TransferStatus.FAILED;
                    transfer!.ApplyProviderResult(
                        _clock.GetUtcNow(), prvCode, providerResult.Error ?? "Provider error", null,
                        providerResult.Status == OutboxStatus.TECHNICAL ? ProviderResultKind.Technical : ProviderResultKind.Error, status);
                    transfer.MarkFailed(DateTimeOffset.UtcNow);

                    var agentForRefund = await _agentRepository.GetForUpdateSqlAsync(transfer.AgentId, ct);
                    if (agentForRefund is not null)
                    {
                        var totalRefund = transfer.CurrentQuote!.Total;
                        var refundRefId = $"{transfer.Id}:Refund";
                        var alreadyRefunded = await _balanceHistory.ExistsByReferenceAsync(agentForRefund.Id, totalRefund.Currency, BalanceHistoryReferenceType.Transfer, refundRefId, ct);
                        if (!alreadyRefunded)
                        {
                            var currentBalance = agentForRefund.Balances.TryGetValue(totalRefund.Currency, out var cr) ? cr : 0L;
                            agentForRefund.Credit(totalRefund.Currency, totalRefund.Minor);
                            var newBalance = agentForRefund.Balances.TryGetValue(totalRefund.Currency, out var up) ? up : currentBalance + totalRefund.Minor;
                            var refundHistory = AgentBalanceHistory.CreateForTransfer(
                                agentForRefund.Id,
                                refundRefId,
                                _clock.GetUtcNow().UtcDateTime,
                                totalRefund.Currency,
                                currentBalance,
                                totalRefund.Minor,
                                newBalance);
                            _balanceHistory.Add(refundHistory);
                        }
                    }
                }
                else
                {
                    _outboxRepository.Add(outbox!);
                }
            }
            else
            {
                _outboxRepository.Add(outbox!);
            }

            await _unitOfWork.CommitChangesAsync(ct);
            return response!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmCommandHandler: unexpected error AgentId={AgentId}, ExternalId={ExternalId}", m.AgentId, m.ExternalId);
            return AppErrors.Common.Unexpected();
        }
    }
}
