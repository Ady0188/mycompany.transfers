using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;
using MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

namespace MyCompany.Transfers.Infrastructure.Providers;

public sealed class PayPorterClient : IProviderClient
{
    public string ProviderId => "PayPorter";
    private readonly IHttpClientFactory _httpFactory;
    private readonly IServiceScopeFactory _scopeFactory;

    public PayPorterClient(IHttpClientFactory httpFactory, IServiceScopeFactory scopeFactory)
    {
        _httpFactory = httpFactory;
        _scopeFactory = scopeFactory;
    }

    public async Task<ProviderResult> SendAsync(Provider provider, ProviderRequest request, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IProviderTokenService>();

        var settings = JsonSerializer.Deserialize<ProviderSettings>(provider.SettingsJson) ?? new ProviderSettings();
        var http = _httpFactory.CreateClient("base");
        http.BaseAddress = new Uri(provider.BaseUrl);
        http.DefaultRequestHeaders.Add("serviceName", "payporter");

        if (!settings.Operations.TryGetValue(request.Operation, out var op))
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                $"Operation '{request.Operation}' not configured for provider '{provider.Id}'");

        var token = await tokenService.GetAccessTokenAsync(provider.Id, ct);
        if (!string.IsNullOrWhiteSpace(token))
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (string.Equals(request.Operation, "create", StringComparison.OrdinalIgnoreCase))
        {
            if (!settings.Operations.TryGetValue("exchange", out _))
                return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                    $"Operation 'exchange' not configured for provider '{provider.Id}'");

            var exchangeResult = await ExchangeAsync(http, request, provider.Id, settings, tokenService, ct);
            if (!exchangeResult.ResponseFields.TryGetValue("exchangeId", out var exchangeId))
                return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(),
                    "No exchangeId from exchange operation");

            if (exchangeResult.Status == OutboxStatus.SENDING)
                return await CreateAsync(http, request, exchangeId, provider.Id, settings, tokenService, ct);
            return exchangeResult;
        }

        if (string.Equals(request.Operation, "status", StringComparison.OrdinalIgnoreCase))
            return await StatusAsync(http, request, provider.Id, settings, tokenService, ct);

        return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
            $"Unsupported operation '{request.Operation}'");
    }

    private async Task<ProviderResult> ExchangeAsync(HttpClient http, ProviderRequest request, string providerId, ProviderSettings pSettings, IProviderTokenService tokenService, CancellationToken ct)
    {
        var op = pSettings.Operations["exchange"];
        var replacements = request.BuildReplacements();
        var body = op.BodyTemplate!.ApplyTemplate(replacements, false, new Dictionary<string, string>());
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await http.PostAsync(op.PathTemplate, content, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var token = await tokenService.RefreshOn401Async(providerId,
                innerCt => LoginAsync(http, pSettings, innerCt), ct);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            content = new StringContent(body, Encoding.UTF8, "application/json");
            response = await http.PostAsync(op.PathTemplate, content, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), "Unauthorized");
        }

        var responseContent = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<ExchangeResponse>(responseContent);
        if (result is null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response");

        var dict = new Dictionary<string, string>
        {
            ["exchangeId"] = result.ResponseObject.ExchangeId,
            ["exchangeRate"] = result.ResponseObject.ExchangeRate.ToString()
        };
        if (!result.Header.Success)
        {
            dict["errorCode"] = result.Header.Message ?? "EXCHANGE_FAILED";
            return new ProviderResult(OutboxStatus.FAILED, dict, result.Header.Message);
        }
        return new ProviderResult(OutboxStatus.SENDING, dict, null);
    }

    private async Task<ProviderResult> CreateAsync(HttpClient http, ProviderRequest request, string exchangeId, string providerId, ProviderSettings pSettings, IProviderTokenService tokenService, CancellationToken ct)
    {
        var op = pSettings.Operations["create"];
        var dict = request.Parameters is not null
            ? new Dictionary<string, string>(request.Parameters, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        dict["ExchangeId"] = exchangeId;
        var replacements = request.BuildReplacements(dict);
        var body = op.BodyTemplate!.ApplyTemplate(replacements, false, new Dictionary<string, string>());
        var content = new StringContent(body, Encoding.UTF8, "application/json");
        var response = await http.PostAsync(op.PathTemplate, content, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var token = await tokenService.RefreshOn401Async(providerId,
                innerCt => LoginAsync(http, pSettings, innerCt), ct);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            content = new StringContent(body, Encoding.UTF8, "application/json");
            response = await http.PostAsync(op.PathTemplate, content, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), "Unauthorized");
        }

        var responseContent = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<CreateResponse>(responseContent);
        if (result is null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response");

        var resultDict = new Dictionary<string, string>();
        if (!result.Header.Success)
        {
            resultDict["errorCode"] = result.Header.Message ?? "CREATE_FAILED";
            return new ProviderResult(OutboxStatus.FAILED, resultDict, result.Header.Message);
        }
        return new ProviderResult(OutboxStatus.SUCCESS, resultDict, null);
    }

    private async Task<ProviderResult> StatusAsync(HttpClient http, ProviderRequest request, string providerId, ProviderSettings pSettings, IProviderTokenService tokenService, CancellationToken ct)
    {
        var op = pSettings.Operations["status"];
        var replacements = request.BuildReplacements();
        var path = op.PathTemplate!.ApplyTemplate(replacements, false, new Dictionary<string, string>());
        var response = await http.GetAsync(path, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var token = await tokenService.RefreshOn401Async(providerId,
                innerCt => LoginAsync(http, pSettings, innerCt), ct);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            response = await http.GetAsync(path, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), "Unauthorized");
        }

        var responseContent = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<CreateResponse>(responseContent);
        if (result is null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response");

        var statusDict = new Dictionary<string, string>();
        if (!result.Header.Success)
        {
            statusDict["errorCode"] = result.Header.Message ?? "STATUS_FAILED";
            return new ProviderResult(OutboxStatus.FAILED, statusDict, result.Header.Message);
        }
        var status = OutboxStatus.SUCCESS;
        if (result.ResponseObject?.Status?.StatusCode is int code && code is not 0 and > 0)
            status = OutboxStatus.STATUS;
        return new ProviderResult(status, statusDict, null);
    }

    private async Task<(string accessToken, DateTimeOffset? expiresAtUtc)> LoginAsync(HttpClient http, ProviderSettings settings, CancellationToken ct)
    {
        var data = new Dictionary<string, string>
        {
            ["username"] = settings.User ?? "",
            ["password"] = settings.Password ?? ""
        };
        http.DefaultRequestHeaders.Authorization = null;
        var response = await http.PostAsync("online/oauth-login", new FormUrlEncodedContent(data), ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        var tokenResp = JsonSerializer.Deserialize<TokenResponse>(content);
        if (tokenResp is null || !tokenResp.Header.Success || string.IsNullOrWhiteSpace(tokenResp.ResponseObject.AccessToken))
            throw new InvalidOperationException("Provider login failed");
        return (tokenResp.ResponseObject.AccessToken, null);
    }
}
