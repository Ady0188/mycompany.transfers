using System.Text;
using Microsoft.Extensions.Logging;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;
using MyCompany.Transfers.Infrastructure.Providers.Responses.TBank;

namespace MyCompany.Transfers.Infrastructure.Providers;

public sealed class TBankClient : IProviderClient
{
    public string ProviderId => "TBank";
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<TBankClient> _logger;

    public TBankClient(IHttpClientFactory httpFactory, ILogger<TBankClient> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<ProviderResult> SendAsync(Provider provider, ProviderRequest request, CancellationToken ct)
    {
        var settings = provider.SettingsJson.Deserialize<ProviderSettings>() ?? new ProviderSettings();
        var http = _httpFactory.CreateClient("base");
        http.BaseAddress = new Uri(provider.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds > 0 ? provider.TimeoutSeconds : 30);
        http.DefaultRequestHeaders.Add("serviceName", "tbank");

        var opName = request.Operation.ToLowerInvariant();
        if (opName is "check" or "prepare")
            return await CheckAsync(http, request, settings, ct);
        if (opName == "confirm")
            return await ConfirmAsync(http, request, settings, ct);
        if (opName == "state")
            return await StateAsync(http, request, settings, ct);

        return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
            $"Operation '{request.Operation}' not configured");
    }

    private async Task<ProviderResult> CheckAsync(HttpClient http, ProviderRequest request, ProviderSettings pSettings, CancellationToken ct)
    {
        var op = pSettings.Operations["check"];
        var replacements = request.BuildReplacements();
        var body = op.BodyTemplate!.ApplyTemplate(replacements, false, new Dictionary<string, string>());
        var response = await http.PostAsync(op.PathTemplate, new StringContent(body, Encoding.UTF8, "application/json"), ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), response.StatusCode.ToString());

        var result = responseContent.Deserialize<TBankCheckResponse>();
        if (result is null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response");

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
            dict["errorCode"] = state ?? "";
            return new ProviderResult(status, dict, result.TransferState?.ErrorMessage ?? "Empty message");
        }
        dict["platform_reference_number"] = result.PlatformReferenceNumber ?? "";
        dict["check_date"] = result.CheckDate ?? "";
        if (result.SettlementAmount is not null)
        {
            dict["settlement_amount"] = result.SettlementAmount.Amount?.ToString()?.Replace(",", ".") ?? "";
            dict["settlement_currency"] = result.SettlementAmount.Currency ?? "RUB";
        }
        if (result.ReceivingAmount is not null)
        {
            dict["receiving_amount"] = result.ReceivingAmount.Amount?.ToString()?.Replace(",", ".") ?? "";
            dict["receiving_currency"] = result.ReceivingAmount.Currency ?? "RUB";
        }
        if (result.FeeAmount is not null)
        {
            var platformFee = result.FeeAmount.FirstOrDefault(x => string.Equals(x.Type, "PLATFORM", StringComparison.OrdinalIgnoreCase));
            if (platformFee is not null)
            {
                dict["platform_fee_amount"] = platformFee.Amount.ToString().Replace(",", ".");
                dict["platform_fee_currency"] = platformFee.Currency ?? "";
            }
            var receiverFee = result.FeeAmount.FirstOrDefault(x => string.Equals(x.Type, "RECEIVER", StringComparison.OrdinalIgnoreCase));
            if (receiverFee is not null)
            {
                dict["receiver_fee_amount"] = receiverFee.Amount.ToString().Replace(",", ".");
                dict["receiver_fee_currency"] = receiverFee.Currency ?? "";
            }
        }
        if (result.ConversionRateBuy is not null)
        {
            dict["conversion_orig_curr"] = result.ConversionRateBuy.OriginatorCurrency;
            dict["conversion_settl_curr"] = result.ConversionRateBuy.SettlementCurrency;
            dict["conversion_rate"] = result.ConversionRateBuy.Rate.ToString().Replace(",", ".");
            dict["conversion_base_rate"] = result.ConversionRateBuy.BaseRate.ToString().Replace(",", ".");
        }
        else
        {
            dict["conversion_orig_curr"] = "RUB";
            dict["conversion_settl_curr"] = "RUB";
            dict["conversion_rate"] = "1";
            dict["conversion_base_rate"] = "1";
        }

        return new ProviderResult(status, dict, "OK");
    }

    private async Task<ProviderResult> ConfirmAsync(HttpClient http, ProviderRequest request, ProviderSettings pSettings, CancellationToken ct)
    {
        var op = pSettings.Operations["confirm"];
        var replacements = request.BuildReplacements();
        var body = op.BodyTemplate!.ApplyTemplate(replacements, false, new Dictionary<string, string>());
        var response = await http.PostAsync(op.PathTemplate, new StringContent(body, Encoding.UTF8, "application/json"), ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), response.StatusCode.ToString());

        var result = responseContent.Deserialize<TBankCheckResponse>();
        if (result is null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response");

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
            dict["errorCode"] = state ?? "";
            return new ProviderResult(status, dict, result.TransferState?.ErrorMessage ?? "Empty message");
        }
        return new ProviderResult(status, dict, "OK");
    }

    private async Task<ProviderResult> StateAsync(HttpClient http, ProviderRequest request, ProviderSettings pSettings, CancellationToken ct)
    {
        var op = pSettings.Operations["state"];
        var replacements = request.BuildReplacements();
        var body = op.BodyTemplate!.ApplyTemplate(replacements, false, new Dictionary<string, string>());
        var response = await http.PostAsync(op.PathTemplate, new StringContent(body, Encoding.UTF8, "application/json"), ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), response.StatusCode.ToString());

        var result = responseContent.Deserialize<TBankStateResponse>();
        if (result is null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response");

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
            dict["errorCode"] = state ?? "";
            return new ProviderResult(status, dict, result.TransferState?.ErrorMessage ?? "Empty message");
        }
        return new ProviderResult(status, dict, "OK");
    }
}
