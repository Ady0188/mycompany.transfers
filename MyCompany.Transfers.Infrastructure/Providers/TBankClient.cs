using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;
using MyCompany.Transfers.Infrastructure.Providers.Responses.TBank;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Text;

namespace MyCompany.Transfers.Infrastructure.Providers;

internal class TBankClient : IProviderClient
{
    public string ProviderId => "TBank";
    private Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpFactory;
    public TBankClient(IServiceScopeFactory scopeFactory, IHttpClientFactory httpFactory)
    {
        _scopeFactory = scopeFactory;
        _httpFactory = httpFactory;
    }

    public async Task<ProviderResult> SendAsync(Provider provider, ProviderRequest request, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var providerHttpHandlerCache = scope.ServiceProvider.GetRequiredService<IProviderHttpHandlerCache>();

        var settings = provider.SettingsJson.Deserialize<ProviderSettings>()
                       ?? new ProviderSettings();

        if (!settings.Operations.TryGetValue(request.Operation, out var op) && request.Operation.ToLower() != "prepare")
        {
            _logger.Warn($"Operation '{request.Operation}' not configured for provider '{provider.Id}'");
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                $"Operation '{request.Operation}' not configured for provider '{provider.Id}'");
        }

        var http = _httpFactory.CreateClient("base");
        http.BaseAddress = new Uri(provider.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds > 0 ? provider.TimeoutSeconds : 30);

        http.DefaultRequestHeaders.Add("serviceName", "tbank");

        var result = new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), null);
        if (request.Operation.ToLower() == "check" || request.Operation.ToLower() == "prepare")
            result = await CheckAsync(http, request, settings, ct);
        if (request.Operation.ToLower() == "confirm")
            result = await ConfirmAsync(http, request, settings, ct);
        if (request.Operation.ToLower() == "state")
            result = await StateAsync(http, request, settings, ct);

        return result;
    }

    private async Task<ProviderResult> CheckAsync(
        HttpClient http,
        ProviderRequest request,
        ProviderSettings pSettings,
        CancellationToken ct)
    {
        var settings = pSettings.Operations["check"];

        var replacements = request.BuildReplacements();
        var body = settings.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

        StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await http.PostAsync(settings.PathTemplate, content);

        _logger.Info($"Response Status: {response.StatusCode}");

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.Info($"Response: {responseContent}");

        if (!response.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), response.StatusCode.ToString());

        var result = responseContent.Deserialize<TBankCheckResponse>();
        if (result == null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response from provider");

        var state = result.TransferState?.State;

        var status = state switch
        {
            "CHECKED" => OutboxStatus.SENDING,
            "CHECK_PENDING" => OutboxStatus.TO_SEND,
            _ => OutboxStatus.FAILED
        };

        var dict = new Dictionary<string, string>();

        if (status == OutboxStatus.FAILED)
        {
            dict["errorCode"] = state;

            return new ProviderResult(status, dict, result.TransferState?.ErrorMessage ?? "Empty message");
        }

        dict["platform_reference_number"] = result.PlatformReferenceNumber!;
        dict["check_date"] = result.CheckDate!;

        var settlementAmount = result.SettlementAmount;
        dict["settlement_amount"] = settlementAmount is null ? "" : settlementAmount.Amount.ToString()!.Replace(",", ".");
        dict["settlement_currency"] = settlementAmount is null ? "RUB" : settlementAmount.Currency;

        var receivingAmount = result.ReceivingAmount;
        dict["receiving_amount"] = receivingAmount is null ? "" : receivingAmount.Amount.ToString()!.Replace(",", ".");
        dict["receiving_currency"] = receivingAmount is null ? "RUB" : receivingAmount.Currency;

        if (result.FeeAmount is not null)
        {
            var platformFee = result.FeeAmount.FirstOrDefault(x => x.Type is not null && x.Type.Equals("PLATFORM", StringComparison.OrdinalIgnoreCase));

            if (platformFee is not null)
            {
                dict["platform_fee_amount"] = platformFee.Amount.ToString().Replace(",", ".")!;
                dict["platform_fee_currency"] = platformFee.Currency!;
            }

            var receiverFee = result.FeeAmount.FirstOrDefault(x => x.Type is not null && x.Type.Equals("RECEIVER", StringComparison.OrdinalIgnoreCase));

            if (receiverFee is not null)
            {
                dict["receiver_fee_amount"] = receiverFee.Amount.ToString().Replace(",", ".")!;
                dict["receiver_fee_currency"] = receiverFee.Currency!;
            }
        }

        if (result.ConversionRateBuy is not null)
        {
            dict["conversion_orig_curr"] = result.ConversionRateBuy.OriginatorCurrency;
            dict["conversion_settl_curr"] = result.ConversionRateBuy.SettlementCurrency;
            dict["conversion_rate"] = result.ConversionRateBuy.Rate.ToString().Replace(",", ".")!;
            dict["conversion_base_rate"] = result.ConversionRateBuy.BaseRate.ToString().Replace(",", ".")!;
        }

        return new ProviderResult(status, dict, "OK");
    }

    private async Task<ProviderResult> ConfirmAsync(
        HttpClient http,
        ProviderRequest request,
        ProviderSettings pSettings,
        CancellationToken ct)
    {
        var settings = pSettings.Operations["confirm"];

        var replacements = request.BuildReplacements();
        var body = settings.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

        StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await http.PostAsync(settings.PathTemplate, content);

        _logger.Info($"Response Status: {response.StatusCode}");

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.Info($"Response: {responseContent}");

        if (!response.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), response.StatusCode.ToString());

        var result = responseContent.Deserialize<TBankCheckResponse>();
        if (result == null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response from provider");

        var state = result.TransferState?.State;

        var status = state switch
        {
            "CONFIRMED" => OutboxStatus.SUCCESS,
            "CONFIRM_PENDING" => OutboxStatus.STATUS,
            _ => OutboxStatus.FAILED
        };
        
        var dict = new Dictionary<string, string>();

        if (status == OutboxStatus.FAILED)
        {
            dict["errorCode"] = state;

            return new ProviderResult(status, dict, result.TransferState?.ErrorMessage ?? "Empty message");
        }

        return new ProviderResult(status, dict, "OK");
    }

    private async Task<ProviderResult> StateAsync(
        HttpClient http,
        ProviderRequest request,
        ProviderSettings pSettings,
        CancellationToken ct)
    {
        var settings = pSettings.Operations["state"];

        var replacements = request.BuildReplacements();
        var body = settings.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

        StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await http.PostAsync(settings.PathTemplate, content);

        _logger.Info($"Response Status: {response.StatusCode}");

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.Info($"Response: {responseContent}");

        if (!response.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), response.StatusCode.ToString());

        var result = responseContent.Deserialize<TBankStateResponse>();
        if (result == null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response from provider");

        var state = result.TransferState?.State;

        var status = state switch
        {
            "CHECKED" => OutboxStatus.SENDING,
            "CHECK_PENDING" => OutboxStatus.TO_SEND,
            "CONFIRMED" => OutboxStatus.SUCCESS,
            "CONFIRM_PENDING" => OutboxStatus.STATUS,
            _ => OutboxStatus.FAILED
        };

        var dict = new Dictionary<string, string>();

        if (status == OutboxStatus.FAILED)
        {
            dict["errorCode"] = state;

            return new ProviderResult(status, dict, result.TransferState?.ErrorMessage ?? "Empty message");
        }

        return new ProviderResult(status, dict, "OK");
    }
}
