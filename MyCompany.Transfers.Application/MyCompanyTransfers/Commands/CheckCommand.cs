using ErrorOr;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Domain.Transfers.Dtos;
using MediatR;
using NLog;

namespace MyCompany.Transfers.Application.MyCompanyTransfers.Commands;

public sealed record CheckCommand(
    string AgentId,
    string ServiceId,
    TransferMethod Method,
    string Account
    //Dictionary<string, string> Parameters
) : IRequest<ErrorOr<CheckResponseDto>>;

public sealed class CheckCommandHandler : IRequestHandler<CheckCommand, ErrorOr<CheckResponseDto>>
{
    private readonly IServiceRepository _services;
    private readonly IProviderService _providerService;
    private readonly IParameterRepository _parameters;
    private readonly IAgentReadRepository _agents;
    private readonly IAccessRepository _access;
    private readonly IFxRateRepository _fxRates;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public CheckCommandHandler(
        IServiceRepository services,
        IParameterRepository parameters,
        IAgentReadRepository agents,
        IAccessRepository access,
        IFxRateRepository fxRates,
        IProviderService providerService)
    {
        _services = services;
        _parameters = parameters;
        _agents = agents;
        _access = access;
        _fxRates = fxRates;
        _providerService = providerService;
    }

    public async Task<ErrorOr<CheckResponseDto>> Handle(CheckCommand m, CancellationToken ct)
    {
        try
        {
            _logger.Info($"CheckCommandHandler.Handle: ServiceId={m.ServiceId}, Account={m.Account}");

            var agent = await _agents.GetForUpdateAsync(m.AgentId, ct);
            if (agent is null)
            {
                _logger.Info($"CheckCommandHandler.Handle: AgentId={m.AgentId}, agent not found");
                return AppErrors.Agents.NotFound(m.AgentId);
            }

            // Access checks
            var access = await _access.GetAgentServiceAccessAsync(m.AgentId, m.ServiceId, ct);
            if (access is null || !access.Enabled)
            {
                _logger.Warn($"CheckCommandHandler.Handle: AgentId={m.AgentId} service not allowed ServiceId={m.ServiceId}");
                return AppErrors.Common.Forbidden($"Агент '{m.AgentId}' не имеет доступа к услуге '{m.ServiceId}'.");
            }

            // 1) Проверка услуги
            var service = await _services.GetByIdAsync(m.ServiceId, ct);
            if (service is null)
            {
                _logger.Warn($"Услуга '{m.ServiceId}' не найдена.");
                return AppErrors.Common.NotFound($"Услуга '{m.ServiceId}' не найдена.");
            }

            if (agent.Balances == null || agent.Balances.Count == 0)
            {
                _logger.Warn($"Для агента '{agent.Id}' не найдено ни одной валюты баланса.");
                return AppErrors.Common.Validation($"Для агента '{agent.Id}' не найдено ни одной валюты баланса.");
            }

            var currency = agent.Balances.First().Key;
            if (!await _access.IsCurrencyAllowedAsync(m.AgentId, currency, ct))
            {
                _logger.Warn($"Агенту '{agent.Id}' недоступна валюта '{currency}'.");
                return AppErrors.Common.Validation($"Агенту '{agent.Id}' недоступна валюта '{currency}'.");
            }

            // 4) Проверяем наличие провайдера
            if (!await _providerService.ExistsEnabledAsync(service.ProviderId, ct))
                return AppErrors.Common.Validation($"Провайдер '{service.ProviderId}' не найден или отключён.");

            // 5) Отправляем запрос провайдеру (минимальный)
            var providerReq = new ProviderRequest(agent.Id, "check", null, 0, null, service.Id, service.ProviderServicveId, m.Account, 0, 0, service.AllowedCurrencies.First(), service.Name, null, null, DateTime.Now);

            var providerResult = await _providerService.SendAsync(service.ProviderId, providerReq, ct);
            if (providerResult.Status != OutboxStatus.SETTING && providerResult.Status != OutboxStatus.SUCCESS)
            {
                return AppErrors.Common.Validation($"Provider check failed: {providerResult.Error}");
            }

            var parameters = new Dictionary<string, string>();

            if (providerResult.ResponseFields.TryGetValue("data.fullname", out var fullname))
            {
                parameters["reciver_fullname"] = fullname;
            }

            List<CurrencyDto> availableCurrencies = new();
            var allowedCurrencies = service.AllowedCurrencies.ToList();
            List<string> clientCurrencies = new();
            if (providerResult.ResponseFields.TryGetValue("data.currencies", out var clientCurrenciesStr))
            {
                clientCurrencies = clientCurrenciesStr.Split(",").ToList() ?? new List<string>();
            }

            if (providerResult.Status != OutboxStatus.SETTING && clientCurrencies.Count() == 0)
            {
                _logger.Warn("");
                return AppErrors.Common.NotFound("Клиент не найден");
            }
            else if (providerResult.Status == OutboxStatus.SETTING)
            {
                clientCurrencies = service.AllowedCurrencies.ToList();
            }

            foreach (var curr in allowedCurrencies)
            {
                if (!clientCurrencies.Contains(curr))
                    continue;

                var fx = await _fxRates.GetAsync(currency, curr, ct);
                if (fx is null)
                    continue;

                availableCurrencies.Add(new CurrencyDto(currency, curr, Math.Round(fx.Value.rate, 4)));
            }

            if (availableCurrencies.Count() == 0)
            {
                _logger.Warn($"Нет доступных валют для услуги '{m.ServiceId}'.");
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
            _logger.Error($"CheckCommandHandler.Handle unexpected error: {ex}");
            return AppErrors.Common.Unexpected();
        }
    }
}