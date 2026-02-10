using ErrorOr;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Services;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Domain.Transfers.Dtos;
using MediatR;
using NLog;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace MyCompany.Transfers.Application.MyCompanyTransfers.Commands;

public sealed record ConfirmCommand(string AgentId, string TerminalId, string ExternalId, string QuotationId)
    : IRequest<ErrorOr<ConfirmResponseDto>>;

public sealed class ConfirmCommandHandler : IRequestHandler<ConfirmCommand, ErrorOr<ConfirmResponseDto>>
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly ITransferRepository _transfers;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IServiceRepository _services;
    private readonly IProviderRepository _providers;
    private readonly IProviderService _providerService;
    private readonly IAgentReadRepository _agentRepository;
    private readonly IUnitOfWork _unitOfWork;
    //private readonly IProviderGateway _providerGateway;
    private readonly TimeProvider _clock;

    public ConfirmCommandHandler(
        ITransferRepository transfers,
        IServiceRepository services,
        IUnitOfWork unitOfWork,
        IProviderRepository providers,
        TimeProvider clock,
        IOutboxRepository outboxRepository,
        IProviderService providerService,
        IAgentReadRepository agentRepository)
    {
        _transfers = transfers;
        _services = services;
        _unitOfWork = unitOfWork;
        _providers = providers;
        _clock = clock;
        _providerService = providerService;
        _outboxRepository = outboxRepository;
        _agentRepository = agentRepository;
    }

    public async Task<ErrorOr<ConfirmResponseDto>> Handle(ConfirmCommand m, CancellationToken ct)
    {
        try
        {
            _logger.Info($"ConfirmCommandHandler.Handle: AgentId={m.AgentId}, ExternalId={m.ExternalId}, QuotationId={m.QuotationId}");

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
                    _logger.Info($"ConfirmCommandHandler.Handle: transfer not found ExternalId={m.ExternalId}");
                    error = AppErrors.Transfers.NotFound(m.ExternalId);
                    return false;
                }

                var agent = await _agentRepository.GetForUpdateAsync(m.AgentId, ct);
                
                switch (transfer.Status)
                {
                    case TransferStatus.PREPARED:
                        break;
                    case TransferStatus.CONFIRMED:
                        _logger.Info($"ConfirmCommandHandler.Handle: AgentId={m.AgentId}, transfer already confirmed ExternalId={m.ExternalId}, Id={transfer.Id}");
                        response = transfer.ToConfirmResponseDto(agent);
                        return true;
                    case TransferStatus.SUCCESS:
                    case TransferStatus.FAILED:
                    case TransferStatus.EXPIRED:
                    case TransferStatus.FRAUD:
                        _logger.Info($"ConfirmCommandHandler.Handle: AgentId={m.AgentId}, transfer already fineshed ExternalId={m.ExternalId}, Id={transfer.Id}");
                        error = AppErrors.Transfers.AlreadyFinished(m.ExternalId, transfer.Status.ToString());
                        return false;
                    default:
                        _logger.Info($"ConfirmCommandHandler.Handle: AgentId={m.AgentId}, transfer in invalid status for confirm ExternalId={m.ExternalId}, Status={transfer.Status}");
                        error = AppErrors.Transfers.NotPrepared(transfer.Status.ToString());
                        return false;
                }

                if (transfer.CurrentQuote is null || transfer.CurrentQuote.Id != m.QuotationId)
                {
                    _logger.Info($"ConfirmCommandHandler.Handle: AgentId={m.AgentId}, quote mismatch ExternalId={m.ExternalId}, QuotationId={m.QuotationId}");
                    error = AppErrors.Transfers.QuoteMismatch; 
                    return false;
                }

                var nowUtc = _clock.GetUtcNow();
                if (transfer.CurrentQuote.IsExpired(nowUtc))
                {
                    _logger.Info($"ConfirmCommandHandler.Handle: AgentId={m.AgentId}, quote expired ExternalId={m.ExternalId}, QuotationId={m.QuotationId}");
                    error = AppErrors.Transfers.QuoteExpired;
                    return false;
                }

                // ВЫЗОВ ВНЕШНЕГО ПРОВАЙДЕРА (новое):
                var service = await _services.GetByIdAsync(transfer.ServiceId, ct);
                if (service is null)
                {
                    _logger.Info($"ConfirmCommandHandler.Handle: AgentId={m.AgentId}, ExternalId={m.ExternalId}, service not found ServiceId={transfer.ServiceId}");
                    error = AppErrors.Transfers.InvalidRequest($"Услуга '{transfer.ServiceId}' не найдена.");
                    return false;
                }

                providerId = service.ProviderId;
                var provider = await _providers.GetAsync(providerId, ct);
                if (provider is null || !provider.IsEnabled)
                {
                    _logger.Warn($"PrepareCommandHandler.Handle: AgentId={m.AgentId}, ExternalId={m.ExternalId} provider not found or disabled ProviderId={service.ProviderId}");
                    error = AppErrors.Common.Validation($"Провайдер '{service.ProviderId}' не найден или отключён.");
                    return false;
                }

                isProviderOnline = provider.IsOnline;

                //var req = new ProviderRequest("confirm",
                //    transfer.Id.ToString("N"), transfer.ExternalId, transfer.ServiceId,
                //    transfer.Account, transfer.CreditedAmount.Minor, transfer.CreditedAmount.Currency, transfer.Parameters);

                //var providerResult = await _providerGateway.SendAsync(service.ProviderId, req, ct);
                //if (!providerResult.Success)
                //{
                //    _logger.Info($"ConfirmCommandHandler.Handle: AgentId={m.AgentId}, ExternalId={m.ExternalId}, provider error: {providerResult.Error}");
                //    error = AppErrors.Common.Validation($"Provider error: {providerResult.Error}");
                //    return false;
                //}

                agent = await _agentRepository.GetForUpdateSqlAsync(m.AgentId, ct);
                if (agent is null)
                {
                    _logger.Info($"ConfirmCommandHandler.Handle: AgentId={m.AgentId}, agent not found");
                    error = AppErrors.Agents.NotFound(m.AgentId);
                    return false;
                }

                var total = transfer.CurrentQuote.Total;
                if (!agent.HasSufficientBalance(total.Currency, total.Minor))
                {
                    _logger.Info($"ConfirmCommandHandler.Handle: AgentId={m.AgentId}, insufficient balance Currency={total.Currency}, RequiredMinor={total.Minor}");
                    error = AppErrors.Agents.InsufficientBalance(total.Currency);
                    return false;
                }

                agent.Debit(total.Currency, total.Minor);
                transfer.MarkConfirmed(nowUtc);

                response = transfer.ToConfirmResponseDto(agent);

                providerReq = new ProviderRequest(agent.Id, "confirm", transfer.Id.ToString(), transfer.NumId, transfer.ExternalId, service.Id, service.ProviderServicveId, transfer.Account, transfer.CurrentQuote!.CreditedAmount.Minor, transfer.CurrentQuote!.ProviderFee.Minor, service.AllowedCurrencies.First(), service.Name, transfer.Parameters, transfer.ProvReceivedParams, transfer.CreatedAtUtc);

                outbox = Outbox.Create(transfer, service, agent.Id);

                return true;
            }, ct);

            if (error is not null)
            {
                _logger.Info($"ConfirmCommandHandler.Handle: returning error: {error.Value}");
                return error.Value;
            }

            if (isProviderOnline)
            {
                var providerResult = await _providerService.SendAsync(providerId, providerReq, ct);

                if (providerResult.Status != OutboxStatus.SUCCESS)
                {
                    transfer!.MarkCompleted(_clock.GetUtcNow(), TransferStatus.SUCCESS);

                    transfer.ApplyProviderResult(
                        _clock.GetUtcNow(),
                        providerCode: "0",
                        description: "OK",
                        providerTransferId: null,
                        kind: ProviderResultKind.Success,
                        status: TransferStatus.SUCCESS);
                }
                else if (providerResult.Status == OutboxStatus.TECHNICAL ||
                    providerResult.Status == OutboxStatus.FAILED)
                {
                    var prvCode = providerResult.ResponseFields.TryGetValue("errorCode", out var c) ? c : "UNKNOWN";
                    var status = providerResult.Status == OutboxStatus.TECHNICAL ? TransferStatus.TECHNICAL : TransferStatus.FAILED;
                    
                    transfer!.ApplyProviderResult(
                        _clock.GetUtcNow(),
                        providerCode: prvCode,
                        providerTransferId: null,
                        description: providerResult.Error ?? "Provider error",
                        kind: providerResult.Status == OutboxStatus.TECHNICAL ? ProviderResultKind.Technical : ProviderResultKind.Error,
                        status: status);

                    transfer.MarkFailed(DateTimeOffset.UtcNow);

                    var agent = await _agentRepository.GetForUpdateSqlAsync(transfer.AgentId, ct);

                    agent!.Credit(transfer!.CurrentQuote!.Total.Currency, transfer.CurrentQuote.Total.Minor);
                }
                else
                    _outboxRepository.Add(outbox!);
            }
            else
                _outboxRepository.Add(outbox!);

            await _unitOfWork.CommitChangesAsync(ct);

            return response!;
        }
        catch (Exception ex)
        {
            _logger.Error($"ConfirmCommandHandler.Handle: unexpected error for AgentId={m.AgentId}, ExternalId={m.ExternalId}: {ex}");
            return AppErrors.Common.Unexpected();
        }
    }
}