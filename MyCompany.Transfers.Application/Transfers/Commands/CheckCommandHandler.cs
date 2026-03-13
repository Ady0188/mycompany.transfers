using System.Text.Json;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Transfers.Commands;

public sealed class CheckCommandHandler : IRequestHandler<CheckCommand, ErrorOr<CheckResponseDto>>
{
    private readonly IServiceRepository _services;
    private readonly IProviderService _providerService;
    private readonly IAgentReadRepository _agents;
    private readonly IAccessRepository _access;
    private readonly IFxRateRepository _fxRates;
    private readonly ILogger<CheckCommandHandler> _logger;

    private static readonly JsonSerializerOptions AgentSettingsJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string OperationKeyCheck = "Check";

    public CheckCommandHandler(
        IServiceRepository services,
        IAgentReadRepository agents,
        IAccessRepository access,
        IFxRateRepository fxRates,
        IProviderService providerService,
        ILogger<CheckCommandHandler> logger)
    {
        _services = services;
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

            var balancesDto = await _agents.GetBalancesAsync(agent.Id, ct);
            if (balancesDto is null || balancesDto.Balances.Count == 0)
            {
                _logger.LogWarning("Для агента '{AgentId}' не найдено ни одной валюты баланса (нет терминалов с балансом).", agent.Id);
                return AppErrors.Common.Validation($"Для агента '{agent.Id}' не найдено ни одной валюты баланса.");
            }

            var currency = balancesDto.Balances.First().Currency;
            if (!await _access.IsCurrencyAllowedAsync(m.AgentId, currency, ct))
            {
                _logger.LogWarning("Агенту '{AgentId}' недоступна валюта '{Currency}'.", agent.Id, currency);
                return AppErrors.Common.Validation($"Агенту '{agent.Id}' недоступна валюта '{currency}'.");
            }

            if (!await _providerService.ExistsEnabledAsync(service.ProviderId, ct))
                return AppErrors.Common.Validation($"Провайдер '{service.ProviderId}' не найден или отключён.");

            var providerReq = new ProviderRequest(Source: agent.Id,
                SourceAccount: string.Empty,
                SourceBankIncomeAccount: null,
                SourceCurrency: currency,
                Destination: string.Empty,
                DestinationAccount: string.Empty,
                DestinationCommissionAccount: null,
                SourceAmount: 0,
                SourceFeeAmount: 0,
                TotalAmount: 0,
                Operation: "check",
                TransferId: string.Empty, 
                NumId: 0,
                ExternalId: string.Empty,
                ServiceId: service.Id,
                ProviderServiceId: service.ProviderServiceId,
                Account: m.Account,
                CreditAmount: 0,
                ProviderFee: 0,
                CurrencyIsoCode: service.AllowedCurrencies.First(),
                ExchangeRate: 0,
                Proc: service.Name,
                Parameters: null,
                ProvReceivedParams: null, 
                TransferDateTime: DateTimeOffset.UtcNow);

            var providerResult = await _providerService.SendAsync(service.ProviderId, providerReq, ct);
            if (providerResult.Status == OutboxStatus.NOT_FOUND)
            {
                return AppErrors.Common.NotFound(providerResult.Error);
            }
            else if (providerResult.Status != OutboxStatus.SETTING && providerResult.Status != OutboxStatus.SUCCESS)
            {
                return AppErrors.Common.Validation($"Provider check failed: {providerResult.Error}");
            }

            var availableCurrencies = new List<CurrencyDto>();
            var allowedCurrencies = service.AllowedCurrencies.ToList();
            var clientAccounts = new List<ClietnAccount>();
            if (!providerResult.ResponseFields.TryGetValue("accounts", out var accounts))
            {
                _logger.LogWarning("CheckCommandHandler: client accounts not found in provider response");
                return AppErrors.Common.NotFound("Счета клиента не найдены в ответе провайдера");
            }

            foreach (var account in accounts.Split(";"))
            {
                var parts = account.Split(",");
                clientAccounts.Add(new ClietnAccount(parts[0],parts[1], long.TryParse(parts[2], out var id) ? id : 0));
            }

            //var clientCurrencies = new List<string>();
            //if (providerResult.ResponseFields.TryGetValue("currencies", out var clientCurrenciesStr))
            //    clientCurrencies = clientCurrenciesStr.Split(",").ToList();

            if (providerResult.Status != OutboxStatus.SETTING && clientAccounts.Count == 0)
            {
                _logger.LogWarning("CheckCommandHandler: client not found");
                return AppErrors.Common.NotFound("Клиент не найден");
            }

            //if (providerResult.Status == OutboxStatus.SETTING)
            //    clientCurrencies = service.AllowedCurrencies.ToList();

            foreach (var curr in allowedCurrencies)
            {
                if (!clientAccounts.Select(x => x.Currency).Contains(curr)) continue;
                var fx = await _fxRates.GetAsync(m.AgentId, currency, curr, ct);
                if (fx is null) continue;
                availableCurrencies.Add(new CurrencyDto(currency, curr, Math.Round(fx.Value.rate, 4)));
            }

            if (availableCurrencies.Count == 0)
            {
                _logger.LogWarning("Нет доступных валют для услуги '{ServiceId}'.", m.ServiceId);
                return AppErrors.Common.Validation($"Нет доступных валют для услуги '{m.ServiceId}'.");
            }

            // ResolvedParameters:
            // 1) значения из стандартизированного ответа провайдера (ResponseFields по Code),
            // 2) дополнительно отфильтрованы по настройкам агента (Agent.SettingsJson -> AgentSettings.ResponseParameters).
            var visibleParamCodes = GetVisibleParameterCodesForAgent(agent, OperationKeyCheck);
            var parameters = BuildResolvedParameters(providerResult.ResponseFields, visibleParamCodes);

            if (availableCurrencies.Count == 1 && visibleParamCodes is not null && visibleParamCodes.Contains("account_number"))
            {
                parameters.Add("account_number", clientAccounts.First(x => x.Currency == availableCurrencies.First().Currency).Number);
                parameters.Add("account_id", clientAccounts.First(x => x.Currency == availableCurrencies.First().Currency).Id.ToString());
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

    /// <summary>
    /// Собирает ResolvedParameters из ответа провайдера, дополнительно фильтруя по списку
    /// разрешённых параметров агента (если он задан).
    /// </summary>
    private static Dictionary<string, string> BuildResolvedParameters(
        IReadOnlyDictionary<string, string> responseFields,
        ISet<string>? allowedCodes)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (responseFields is null) return parameters;

        foreach (var (code, value) in responseFields)
        {
            if (string.IsNullOrWhiteSpace(code)) continue;
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (allowedCodes is not null && !allowedCodes.Contains(code))
                continue;

            parameters[code] = value;
        }

        return parameters;
    }

    /// <summary>
    /// Возвращает множество кодов параметров (ParamDefinition.Code), которые разрешено отдавать агенту
    /// для указанной операции (Command/Query). Если настроек нет или список пустой, возвращает null,
    /// что означает "без дополнительной фильтрации по агенту".
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
            // Некорректный JSON в настройках агента — не ломаем обработку, просто не применяем фильтрацию.
            return null;
        }

        if (settings is null || settings.ResponseParameters is null || settings.ResponseParameters.Count == 0)
            return null;

        if (!settings.ResponseParameters.TryGetValue(operationKey, out var codes) || codes is null || codes.Length == 0)
            return null;

        return new HashSet<string>(codes.Where(c => !string.IsNullOrWhiteSpace(c)), StringComparer.OrdinalIgnoreCase);
    }
}

