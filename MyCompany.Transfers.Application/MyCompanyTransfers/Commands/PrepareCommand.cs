using ErrorOr;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Accounts;
using MyCompany.Transfers.Domain.Common;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Domain.Transfers.Dtos;
using MediatR;
using NLog;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MyCompany.Transfers.Application.MyCompanyTransfers.Commands;
public sealed record PrepareCommand(string AgentId, string TerminalId, string ExternalId, TransferMethod Method, string Account, long Amount, string Currency, string? PayoutCurrency, string ServiceId, Dictionary<string, string> Parameters)
    : IRequest<ErrorOr<PrepareResponseDto>>;
public sealed class PrepareCommandHandler : IRequestHandler<PrepareCommand, ErrorOr<PrepareResponseDto>>
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly ITransferRepository _transfers;
    private readonly IAgentReadRepository _agents;
    private readonly IProviderService _providerService;
    private readonly IServiceRepository _services;
    private readonly IParameterRepository _paramRepo;
    private readonly IAccountDefinitionRepository _accountDefRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessRepository _access;
    private readonly IFxRateRepository _fxRates;
    private readonly ICurrencyConverter _converter;
    private readonly IProviderRepository _providers;
    private readonly TimeProvider _clock;

    public PrepareCommandHandler(
        ITransferRepository transfers,
        IAgentReadRepository agents,
        IServiceRepository services,
        IUnitOfWork unitOfWork,
        TimeProvider clock,
        IAccessRepository access,
        IProviderRepository providers,
        IFxRateRepository fxRates,
        ICurrencyConverter converter,
        IParameterRepository paramRepo,
        IAccountDefinitionRepository accountDefRepo,
        IProviderService providerService)
    {
        _transfers = transfers;
        _agents = agents;
        _services = services;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _access = access;
        _providers = providers;
        _fxRates = fxRates;
        _converter = converter;
        _paramRepo = paramRepo;
        _accountDefRepo = accountDefRepo;
        _providerService = providerService;
    }

    public async Task<ErrorOr<PrepareResponseDto>> Handle(PrepareCommand request, CancellationToken ct)
    {
        try
        {
            _logger.Info($"PrepareCommandHandler.Handle: AgentId={request.AgentId}, ExternalId={request.ExternalId}, ServiceId={request.ServiceId}, Amount={request.Amount}, Currency={request.Currency}");
            
            var agent = await _agents.GetByIdAsync(request.AgentId, ct);
            if (agent is null)
            {
                _logger.Info($"PrepareCommandHandler.Handle: AgentId={request.AgentId}, agent not found");
                return AppErrors.Agents.NotFound(request.AgentId);
            }

            var existing = await _transfers.FindByExternalIdAsync(request.AgentId, request.ExternalId, ct);
            if (existing is not null)
            {
                _logger.Info($"PrepareCommandHandler.Handle: existing transfer found Id={existing.Id}, Status={existing.Status}");
                return AppErrors.Transfers.ExternalIdConflict(request.ExternalId);
                //return existing.ToPrepareResponseDto(agent);
            }

            // Access checks
            var access = await _access.GetAgentServiceAccessAsync(request.AgentId, request.ServiceId, ct);
            if (access is null || !access.Enabled)
            {
                _logger.Warn($"PrepareCommandHandler.Handle: AgentId={request.AgentId}, ExternalId={request.ExternalId} service not allowed ServiceId={request.ServiceId}");
                return AppErrors.Common.Forbidden($"Агент '{request.AgentId}' не имеет доступа к услуге '{request.ServiceId}'.");
            }

            if (!await _access.IsCurrencyAllowedAsync(request.AgentId, request.Currency, ct))
            {
                _logger.Warn($"PrepareCommandHandler.Handle: AgentId={request.AgentId}, ExternalId={request.ExternalId} currency not allowed Currency={request.Currency}");
                return AppErrors.Common.Forbidden($"Агенту '{request.AgentId}' недоступна валюта '{request.Currency}'.");
            }

            var nowUtc = _clock.GetUtcNow();
            var feeMinor = access.CalculateFee(request.Amount);
            var totalMinor = checked(request.Amount + feeMinor);

            if (!agent.HasSufficientBalance(request.Currency, totalMinor))
            {
                _logger.Warn($"PrepareCommandHandler.Handle: AgentId={request.AgentId}, ExternalId={request.ExternalId} insufficient balance Currency={request.Currency}, Amount={request.Amount}");
                return AppErrors.Agents.InsufficientBalance(request.Currency);
            }

            // 2) проверка услуги
            var service = await _services.GetByIdAsync(request.ServiceId, ct);
            if (service is null)
            {
                _logger.Warn($"PrepareCommandHandler.Handle: AgentId={request.AgentId}, ExternalId={request.ExternalId} service not found ServiceId={request.ServiceId}");
                return AppErrors.Transfers.InvalidRequest($"Услуга '{request.ServiceId}' не найдена.");
            }

            var account = request.Account;
            var accountDef = await _accountDefRepo.GetAsync(service.AccountDefinitionId, ct);
            if (accountDef is null)
            {
                _logger.Warn($"PrepareCommandHandler.Handle: AgentId={request.AgentId}, ExternalId={request.ExternalId} account definition not found AccountDefinitionId={service.AccountDefinitionId}");
                return AppErrors.Common.Validation($"Определение счёта '{service.AccountDefinitionId}' не найдено.");
            }

            account = AccountRules.Normalize(request.Account, accountDef);
            var catalog = await _paramRepo.GetAllAsync(ct);
            try
            {
                AccountRules.Validate(account, accountDef);
                service.ValidateParameters(request.Parameters, catalog);
            }
            catch (DomainException ex)
            {
                _logger.Warn($"PrepareCommandHandler.Handle: AgentId={request.AgentId}, ExternalId={request.ExternalId} validation failed: {ex}");
                return AppErrors.Common.Validation(ex.Message);
            }

            var provider = await _providers.GetAsync(service.ProviderId, ct);
            if (provider is null || !provider.IsEnabled)
            {
                _logger.Warn($"PrepareCommandHandler.Handle: AgentId={request.AgentId}, ExternalId={request.ExternalId} provider not found or disabled ProviderId={service.ProviderId}");
                return AppErrors.Common.Validation($"Провайдер '{service.ProviderId}' не найден или отключён.");
            }

            // 4) агент + баланс
            //var agent = await _agents.GetForUpdateAsync(m.AgentId, ct);
            //if (agent is null)
            //{
            //    _logger.Warn($"PrepareCommandHandler.Handle: AgentId={m.AgentId}, ExternalId={m.ExternalId} agent not found");
            //    return AppErrors.Agents.NotFound(m.AgentId);
            //}


            //// 5) котировка
            //var nowUtc = _clock.GetUtcNow();
            //var feeMinor = access.CalculateFee(m.Amount);
            //var totalMinor = checked(m.Amount + feeMinor);

            //if (!agent.HasSufficientBalance(m.Currency, totalMinor))
            //{
            //    _logger.Warn($"PrepareCommandHandler.Handle: AgentId={m.AgentId}, ExternalId={m.ExternalId} insufficient balance Currency={m.Currency}, Amount={m.Amount}");
            //    return AppErrors.Agents.InsufficientBalance(m.Currency);
            //}

            #region Conversation

            var dstCurrency = request.PayoutCurrency;
            if (string.IsNullOrEmpty(dstCurrency))
                dstCurrency = service.AllowedCurrencies.First();
            else if (!service.AllowedCurrencies.Contains(dstCurrency))
            {
                _logger.Warn($"PrepareCommandHandler.Handle: There not avalable currency '{dstCurrency}' for current service");
                return AppErrors.Common.Validation($"There not avalable currency '{dstCurrency}' for current service''");
            }

            var fx = await _fxRates.GetAsync(request.Currency, dstCurrency, ct);
            if (fx is null)
                return AppErrors.Common.Validation($"FX rate not found for {request.Currency}->{dstCurrency}");

            var (rate, asOfUtc) = fx.Value;

            var srcMinorUnit = 2;
            var dstMinorUnit = 2;
            var creditedMinor = _converter.ConvertMinor(
                srcMinor: request.Amount,
                srcMinorUnit: srcMinorUnit,
                dstMinorUnit: dstMinorUnit,
                rate: rate,
                rounding: service.FxRounding ?? "floor");

            var providerFeeMinor = provider.CalculateFee(creditedMinor);

            var total = new Money(totalMinor, request.Currency);
            var fee = new Money(feeMinor, request.Currency);

            var providerFee = new Money(providerFeeMinor, dstCurrency);
            var creditedAmount = new Money(creditedMinor, dstCurrency);

            #endregion

            Quote quote;
            try
            {
                quote = Quote.Create(total, fee, providerFee, creditedAmount, rate, asOfUtc, ttl: TimeSpan.FromMinutes(5), now: nowUtc);
            }
            catch (DomainException ex)
            {
                _logger.Warn($"PrepareCommandHandler.Handle: quote create failed: {ex}");
                return AppErrors.Common.Validation(ex.Message);
            }

            // 6) создать/обновить агрегат
            var transfer = existing ?? Transfer.CreatePrepare(
                agentId: request.AgentId,
                terminalId: request.TerminalId,
                externalId: request.ExternalId,
                serviceId: request.ServiceId,
                method: request.Method,
                account: account,
                amountMinor: request.Amount,
                currency: request.Currency,
                parameters: request.Parameters,
                quote: quote);

            // Add only if new
            if (existing is null)
                _transfers.Add(transfer);

            await _unitOfWork.CommitChangesAsync(ct);

            if (provider.IsOnline)
            {
                var providerReq = new ProviderRequest(agent.Id, "prepare", transfer.Id.ToString(), transfer.NumId, transfer.ExternalId, service.Id, service.ProviderServicveId, transfer.Account, transfer.CurrentQuote!.CreditedAmount.Minor, transfer.CurrentQuote!.ProviderFee.Minor, service.AllowedCurrencies.First(), service.Name, transfer.Parameters, null, transfer.CreatedAtUtc);

                var providerResult = await _providerService.SendAsync(service.ProviderId, providerReq, ct);

                if (providerResult.Status == OutboxStatus.SUCCESS)
                {
                    transfer.SetReceivedParams(providerResult.ResponseFields);

                    transfer.ApplyProviderResult(
                        _clock.GetUtcNow(),
                        providerCode: "0",
                        description: "OK",
                        providerTransferId: null,
                        kind: ProviderResultKind.Success,
                        status: TransferStatus.PREPARED);
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
                }

                _transfers.Update(transfer);
                await _unitOfWork.CommitChangesAsync(ct);
            }

            _logger.Info($"PrepareCommandHandler.Handle: prepared transfer ExternalId={request.ExternalId}, Id={transfer.Id}, Status={transfer.Status}");
            return transfer.ToPrepareResponseDto(agent);
        }
        catch (Exception ex)
        {
            _logger.Error($"PrepareCommandHandler.Handle Error: {ex}");
            return AppErrors.Common.Unexpected();
        }
    }
}