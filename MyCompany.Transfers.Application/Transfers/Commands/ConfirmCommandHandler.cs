using System.Text.Json;
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
    private readonly ITerminalRepository _terminalRepository;
    private readonly IAgentBalanceHistoryRepository _balanceHistory;
    private readonly IBinRepository _binRepository;
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
        ITerminalRepository terminalRepository,
        IAgentBalanceHistoryRepository balanceHistory,
        ILogger<ConfirmCommandHandler> logger,
        IBinRepository binRepository)
    {
        _transfers = transfers;
        _services = services;
        _unitOfWork = unitOfWork;
        _providers = providers;
        _clock = clock;
        _outboxRepository = outboxRepository;
        _providerService = providerService;
        _agentRepository = agentRepository;
        _terminalRepository = terminalRepository;
        _balanceHistory = balanceHistory;
        _logger = logger;
        _binRepository = binRepository;
    }

    private static readonly JsonSerializerOptions AgentSettingsJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string OperationKeyConfirm = "Confirm";

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
                        var termForResponse = await _terminalRepository.GetAsync(transfer.TerminalId, ct);

                        // Для повторного Confirm возвращаем те же ResolvedParameters,
                        // отфильтрованные по текущим настройкам агента.
                        var existingService = await _services.GetByIdAsync(transfer.ServiceId, ct);
                        var visibleCodesExisting = existingService is null
                            ? null
                            : GetVisibleParameterCodesForAgent(agent!, OperationKeyConfirm);
                        var resolvedExisting = existingService is null
                            ? new Dictionary<string, string>(transfer.Parameters)
                            : BuildResolvedParameters(existingService, transfer.Parameters, transfer.ProvReceivedParams ?? new Dictionary<string, string>(), visibleCodesExisting);

                        response = transfer.ToConfirmResponseDto(agent!, termForResponse?.BalanceMinor ?? 0, resolvedExisting);
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

                var binInfo = await _binRepository.GetByCodeAsync("IBT", ct);

                var acc = transfer.Account ?? string.Empty;
                var isPan = acc.Length == 16;

                var isAllowedNonIbtProvider = providerId == "IPS" || providerId == "FIMI";

                var isIBTByPan =
                    isPan &&
                    (
                        providerId == "IBT" ||
                        (isAllowedNonIbtProvider && binInfo.Any(b => acc.StartsWith(b.Prefix)))
                    );
                var isIBTByPhone = providerId == "IBT" && acc.Length < 16;

                agent = await _agentRepository.GetForUpdateSqlAsync(m.AgentId, ct);
                if (agent is null)
                {
                    _logger.LogInformation("ConfirmCommandHandler: agent not found AgentId={AgentId}", m.AgentId);
                    error = AppErrors.Agents.NotFound(m.AgentId);
                    return false;
                }

                var terminal = await _terminalRepository.GetForUpdateAsync(transfer.TerminalId, ct);
                if (terminal is null || terminal.AgentId != m.AgentId)
                {
                    _logger.LogInformation("ConfirmCommandHandler: terminal not found or not belongs to agent TerminalId={TerminalId}", transfer.TerminalId);
                    error = AppErrors.Common.Validation("Терминал перевода не найден.");
                    return false;
                }

                var total = transfer.CurrentQuote.Total;
                if (!terminal.HasSufficientBalance(total.Minor))
                {
                    _logger.LogInformation("ConfirmCommandHandler: insufficient balance Currency={Currency}, RequiredMinor={Minor}", total.Currency, total.Minor);
                    error = AppErrors.Agents.InsufficientBalance(total.Currency);
                    return false;
                }

                var debitRefId = $"{transfer.Id}:Debit";
                var alreadyDebited = await _balanceHistory.ExistsByReferenceAsync(terminal.Id, BalanceHistoryReferenceType.Transfer, debitRefId, ct);
                if (!alreadyDebited)
                {
                    var currentBalance = terminal.BalanceMinor;
                    terminal.Debit(total.Minor);
                    _terminalRepository.Update(terminal);
                    var newBalance = terminal.BalanceMinor;
                    var history = AgentBalanceHistory.CreateForTransfer(
                        agent.Id,
                        terminal.Id,
                        debitRefId,
                        nowUtc.UtcDateTime,
                        total.Currency,
                        currentBalance,
                        -total.Minor,
                        newBalance);
                    _balanceHistory.Add(history);
                }

                transfer.MarkConfirmed(nowUtc);

                providerReq = new ProviderRequest(Source: agent.Id,
                    SourceAccount: string.Empty,
                    SourceBankIncomeAccount: null,
                    SourceCurrency: string.Empty,
                    Destination: string.Empty,
                    DestinationAccount: string.Empty,
                    DestinationCommissionAccount: null,
                    SourceAmount: 0,
                    SourceFeeAmount: 0,
                    TotalAmount: 0,
                    Operation: "confirm", 
                    TransferId: transfer.Id.ToString(),
                    NumId: transfer.NumId, 
                    ExternalId: transfer.ExternalId,
                    ServiceId: service.Id, 
                    ProviderServiceId: service.ProviderServiceId, 
                    Account: transfer.Account ?? string.Empty,
                    CreditAmount: transfer.CurrentQuote!.CreditedAmount.Minor,
                    ProviderFee: transfer.CurrentQuote!.ProviderFee.Minor,
                    CurrencyIsoCode: service.AllowedCurrencies.First(),
                    ExchangeRate: 0,
                    Proc: service.Name,
                    Parameters: transfer.Parameters,
                    ProvReceivedParams: transfer.ProvReceivedParams,
                    TransferDateTime: transfer.CreatedAtUtc);

                outbox = Outbox.Create(transfer, service, agent.Id);

                if (isIBTByPan || isIBTByPhone)
                {
                    transfer.MarkCompleted(_clock.GetUtcNow(), TransferStatus.SUCCESS);
                    outbox.MarkCompleted(_clock.GetUtcNow(), OutboxStatus.SUCCESS);
                }

                // ResolvedParameters для Confirm:
                // 1) параметры услуги (ServiceParamDefinition -> ParamDefinition.Code),
                // 2) значения как из параметров перевода (transfer.Parameters), так и из полученных от провайдера (ProvReceivedParams),
                // 3) (опционально) фильтрация по настройкам агента.
                var visibleParamCodes = GetVisibleParameterCodesForAgent(agent, OperationKeyConfirm);
                var resolvedParameters = BuildResolvedParameters(service, transfer.Parameters, transfer.ProvReceivedParams ?? new Dictionary<string, string>(), visibleParamCodes);

                response = transfer.ToConfirmResponseDto(agent, terminal.BalanceMinor, resolvedParameters);

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

                    var terminalForRefund = await _terminalRepository.GetForUpdateAsync(transfer.TerminalId, ct);
                    if (terminalForRefund is not null)
                    {
                        var totalRefund = transfer.CurrentQuote!.Total;
                        var refundRefId = $"{transfer.Id}:Refund";
                        var alreadyRefunded = await _balanceHistory.ExistsByReferenceAsync(terminalForRefund.Id, BalanceHistoryReferenceType.Transfer, refundRefId, ct);
                        if (!alreadyRefunded)
                        {
                            var currentBalance = terminalForRefund.BalanceMinor;
                            terminalForRefund.Credit(totalRefund.Minor);
                            _terminalRepository.Update(terminalForRefund);
                            var newBalance = terminalForRefund.BalanceMinor;
                            var refundHistory = AgentBalanceHistory.CreateForTransfer(
                                transfer.AgentId,
                                terminalForRefund.Id,
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
    /// <summary>
    /// Собирает ResolvedParameters для Confirm:
    /// приоритетно берём значения из ProvReceivedParams (ответ провайдера),
    /// при отсутствии — из параметров перевода (Parameters),
    /// и дополнительно фильтруем по списку разрешённых кодов агента.
    /// </summary>
    private static Dictionary<string, string> BuildResolvedParameters(
        Domain.Services.Service service,
        IReadOnlyDictionary<string, string> transferParameters,
        IReadOnlyDictionary<string, string> provReceivedParams,
        ISet<string>? allowedCodes)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (service.Parameters is null) return parameters;

        foreach (var serviceParam in service.Parameters)
        {
            var code = serviceParam.Parameter?.Code;
            if (string.IsNullOrWhiteSpace(code)) continue;

            if (allowedCodes is not null && !allowedCodes.Contains(code))
                continue;

            string? value = null;
            if (provReceivedParams.TryGetValue(code, out var fromProv) && !string.IsNullOrWhiteSpace(fromProv))
            {
                value = fromProv;
            }
            else if (transferParameters.TryGetValue(code, out var fromTransfer) && !string.IsNullOrWhiteSpace(fromTransfer))
            {
                value = fromTransfer;
            }

            if (string.IsNullOrWhiteSpace(value))
                continue;

            parameters[code] = value;
        }

        return parameters;
    }

    /// <summary>
    /// Возвращает множество кодов параметров (ParamDefinition.Code), разрешённых агенту для указанной операции.
    /// null означает отсутствие дополнительной фильтрации по агенту.
    /// </summary>
    private static ISet<string>? GetVisibleParameterCodesForAgent(Agent agent, string operationKey)
    {
        if (string.IsNullOrWhiteSpace(agent.SettingsJson))
            return null;

        AgentSettings? settings;
        try
        {
            settings = JsonSerializer.Deserialize<AgentSettings>(agent.SettingsJson, AgentSettingsJsonOptions);
        }
        catch
        {
            return null;
        }

        if (settings is null || settings.ResponseParameters is null || settings.ResponseParameters.Count == 0)
            return null;

        if (!settings.ResponseParameters.TryGetValue(operationKey, out var codes) || codes is null || codes.Length == 0)
            return null;

        return new HashSet<string>(codes.Where(c => !string.IsNullOrWhiteSpace(c)), StringComparer.OrdinalIgnoreCase);
    }
}
