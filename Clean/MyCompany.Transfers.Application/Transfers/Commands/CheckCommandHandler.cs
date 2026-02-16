using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Transfers.Commands;

public sealed class CheckCommandHandler : IRequestHandler<CheckCommand, ErrorOr<CheckResponseDto>>
{
    private readonly IServiceRepository _services;
    private readonly IProviderService _providerService;
    private readonly IParameterRepository _parameters;
    private readonly IAgentReadRepository _agents;
    private readonly IAccessRepository _access;
    private readonly IFxRateRepository _fxRates;
    private readonly ILogger<CheckCommandHandler> _logger;

    public CheckCommandHandler(
        IServiceRepository services,
        IParameterRepository parameters,
        IAgentReadRepository agents,
        IAccessRepository access,
        IFxRateRepository fxRates,
        IProviderService providerService,
        ILogger<CheckCommandHandler> logger)
    {
        _services = services;
        _parameters = parameters;
        _agents = agents;
        _access = access;
        _fxRates = fxRates;
        _providerService = providerService;
        _logger = logger;
    }

    public async Task<ErrorOr<CheckResponseDto>> Handle(CheckCommand m, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("CheckCommandHandler: ServiceId={ServiceId}, Account={Account}", m.ServiceId, m.Account);

            var agent = await _agents.GetForUpdateAsync(m.AgentId, ct);
            if (agent is null)
            {
                _logger.LogInformation("CheckCommandHandler: AgentId={AgentId}, agent not found", m.AgentId);
                return AppErrors.Agents.NotFound(m.AgentId);
            }

            var access = await _access.GetAgentServiceAccessAsync(m.AgentId, m.ServiceId, ct);
            if (access is null || !access.Enabled)
            {
                _logger.LogWarning("CheckCommandHandler: AgentId={AgentId} service not allowed ServiceId={ServiceId}", m.AgentId, m.ServiceId);
                return AppErrors.Common.Forbidden($"Агент '{m.AgentId}' не имеет доступа к услуге '{m.ServiceId}'.");
            }

            var service = await _services.GetByIdAsync(m.ServiceId, ct);
            if (service is null)
            {
                _logger.LogWarning("Услуга '{ServiceId}' не найдена.", m.ServiceId);
                return AppErrors.Common.NotFound($"Услуга '{m.ServiceId}' не найдена.");
            }

            if (agent.Balances is null || agent.Balances.Count == 0)
            {
                _logger.LogWarning("Для агента '{AgentId}' не найдено ни одной валюты баланса.", agent.Id);
                return AppErrors.Common.Validation($"Для агента '{agent.Id}' не найдено ни одной валюты баланса.");
            }

            var currency = agent.Balances.Keys.First();
            if (!await _access.IsCurrencyAllowedAsync(m.AgentId, currency, ct))
            {
                _logger.LogWarning("Агенту '{AgentId}' недоступна валюта '{Currency}'.", agent.Id, currency);
                return AppErrors.Common.Validation($"Агенту '{agent.Id}' недоступна валюта '{currency}'.");
            }

            if (!await _providerService.ExistsEnabledAsync(service.ProviderId, ct))
                return AppErrors.Common.Validation($"Провайдер '{service.ProviderId}' не найден или отключён.");

            var providerReq = new ProviderRequest(
                agent.Id, "check", string.Empty, 0, string.Empty, service.Id, service.ProviderServiceId, m.Account, 0, 0,
                service.AllowedCurrencies.First(), service.Name, null, null, DateTimeOffset.UtcNow);

            var providerResult = await _providerService.SendAsync(service.ProviderId, providerReq, ct);
            if (providerResult.Status != OutboxStatus.SETTING && providerResult.Status != OutboxStatus.SUCCESS)
            {
                return AppErrors.Common.Validation($"Provider check failed: {providerResult.Error}");
            }

            var parameters = new Dictionary<string, string>();
            if (providerResult.ResponseFields.TryGetValue("data.fullname", out var fullname))
                parameters["reciver_fullname"] = fullname;

            var availableCurrencies = new List<CurrencyDto>();
            var allowedCurrencies = service.AllowedCurrencies.ToList();
            var clientCurrencies = new List<string>();
            if (providerResult.ResponseFields.TryGetValue("data.currencies", out var clientCurrenciesStr))
                clientCurrencies = clientCurrenciesStr.Split(",").ToList();

            if (providerResult.Status != OutboxStatus.SETTING && clientCurrencies.Count == 0)
            {
                _logger.LogWarning("CheckCommandHandler: client not found");
                return AppErrors.Common.NotFound("Клиент не найден");
            }

            if (providerResult.Status == OutboxStatus.SETTING)
                clientCurrencies = service.AllowedCurrencies.ToList();

            foreach (var curr in allowedCurrencies)
            {
                if (!clientCurrencies.Contains(curr)) continue;
                var fx = await _fxRates.GetAsync(m.AgentId, currency, curr, ct);
                if (fx is null) continue;
                availableCurrencies.Add(new CurrencyDto(currency, curr, Math.Round(fx.Value.rate, 4)));
            }

            if (availableCurrencies.Count == 0)
            {
                _logger.LogWarning("Нет доступных валют для услуги '{ServiceId}'.", m.ServiceId);
                return AppErrors.Common.Validation($"Нет доступных валют для услуги '{m.ServiceId}'.");
            }

            return new CheckResponseDto
            {
                AvailableCurrencies = availableCurrencies,
                ResolvedParameters = parameters
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckCommandHandler unexpected error");
            return AppErrors.Common.Unexpected();
        }
    }
}
