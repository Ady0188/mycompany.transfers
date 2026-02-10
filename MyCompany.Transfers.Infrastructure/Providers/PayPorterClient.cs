using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;
using MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Text;
using System.Text.Json;

namespace MyCompany.Transfers.Infrastructure.Providers;

internal sealed class PayProrterClient : IProviderClient
{
    public string ProviderId => "PayPorter";
    private Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly IHttpClientFactory _httpFactory;
    private readonly IProviderTokenService _providerTokenService;

    public PayProrterClient(IHttpClientFactory httpFactory, IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        _providerTokenService = scope.ServiceProvider.GetRequiredService<IProviderTokenService>();

        _httpFactory = httpFactory;
    }

    public async Task<ProviderResult> SendAsync(Provider provider, ProviderRequest request, CancellationToken ct)
    {
        var settings = provider.SettingsJson.Deserialize<ProviderSettings>()
                       ?? new ProviderSettings();


        var http = _httpFactory.CreateClient("base");
        http.BaseAddress = new Uri(provider.BaseUrl);
        http.DefaultRequestHeaders.Add("serviceName", "payporter");

        if (!settings.Operations.TryGetValue(request.Operation, out var op))
        {
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                $"Operation '{request.Operation}' not configured for provider '{provider.Id}'");
        }

        var token = await _providerTokenService.GetAccessTokenAsync(provider.Id, ct);
        if (!string.IsNullOrWhiteSpace(token))
            SetBearer(http, token);

        var result = new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), null);
        if (request.Operation.ToLower() == "create")
        {
            if (!settings.Operations.TryGetValue("exchange", out var exchangeOperation))
            {
                return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                    $"Operation 'exchange' not configured for provider '{provider.Id}'");
            }

            result = await ExchangeAsync(http, request, provider.Id, settings, ct);

            if (!result.ResponseFields.TryGetValue("exchangeId", out var exchangeId))
            {
                return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(),
                    $"No exchangeId returned from exchange operation for provider '{provider.Id}'");
            }

            if (result.Status == OutboxStatus.SUCCESS)
                result = await CreateAsync(http, request, exchangeId, provider.Id, settings, ct);

        }
        else if (request.Operation.ToLower() == "status")
            result = await StatusAsync(http, request, provider.Id, settings, ct);

        return result;
    }

    public async Task<ProviderResult> ExchangeAsync(HttpClient http, ProviderRequest request, string providerId, ProviderSettings pSettings, CancellationToken ct)
    {
        var settings = pSettings.Operations["exchange"];

        var replacements = request.BuildReplacements();
        var body = settings.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

        StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await http.PostAsync(settings.PathTemplate, content);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var token = await _providerTokenService.RefreshOn401Async(
                providerId,
                loginFunc: async innerCt =>
                {
                    return await LoginAsync(http, pSettings, innerCt);
                },
                ct);

            SetBearer(http, token);

            response = await http.PostAsync(settings.PathTemplate, content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), "Unauthorized");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ExchangeResponse>(responseContent);
        if (result == null) 
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response from provider");

        var dict = new Dictionary<string, string>
        {
            { "exchangeId", result.ResponseObject.ExchangeId },
            { "exchangeRate", result.ResponseObject.ExchangeRate.ToString() }
        };

        var status = result.Header.Success ? OutboxStatus.SUCCESS : OutboxStatus.FAILED;
        return new ProviderResult(status, dict, result.Header.Message);
    }

    public async Task<ProviderResult> CreateAsync(HttpClient http, ProviderRequest request, string providerId, string exchangeId, ProviderSettings pSettings, CancellationToken ct)
    {
        var settings = pSettings.Operations["create"];

        var dict = request.Parameters.ToDictionary();
        dict.Add("ExchangeId", exchangeId);

        var replacements = request.BuildReplacements(dict);
        var body = settings.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

        StringContent content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await http.PostAsync(settings.PathTemplate, content);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var token = await _providerTokenService.RefreshOn401Async(
                providerId,
                loginFunc: async innerCt =>
                {
                    return await LoginAsync(http, pSettings, innerCt);
                },
                ct);

            SetBearer(http, token);

            response = await http.PostAsync(settings.PathTemplate, content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), "Unauthorized");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreateResponse>(responseContent);
        if (result == null) 
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response from provider");

        var status = result.Header.Success ? OutboxStatus.SUCCESS : OutboxStatus.FAILED;
        return new ProviderResult(status, new Dictionary<string, string>(), result.Header.Message);
    }

    public async Task<ProviderResult> StatusAsync(HttpClient http, ProviderRequest request, string providerId, ProviderSettings pSettings, CancellationToken ct)
    {
        var settings = pSettings.Operations["status"];

        var replacements = request.BuildReplacements();
        var path = settings.PathTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

        var response = await http.GetAsync(path, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var token = await _providerTokenService.RefreshOn401Async(
                providerId,
                loginFunc: async innerCt =>
                {
                    return await LoginAsync(http, pSettings, innerCt);
                },
                ct);

            SetBearer(http, token);

            response = await http.GetAsync(path, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), "Unauthorized");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreateResponse>(responseContent);
        if (result == null) 
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response from provider");

        var status = result.Header.Success ? OutboxStatus.SUCCESS : OutboxStatus.FAILED;
        return new ProviderResult(status, new Dictionary<string, string>(), result.Header.Message);
    }

    private static void SetBearer(HttpClient http, string token)
    {
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<(string accessToken, DateTimeOffset? expiresAtUtc)> LoginAsync(
    HttpClient http,
    ProviderSettings settings,
    CancellationToken ct)
    {
        var data = new Dictionary<string, string>
        {
            { "username", settings.User! },
            { "password", settings.Password! }
        };

        if (http.DefaultRequestHeaders.Authorization != null)
            http.DefaultRequestHeaders.Authorization = null;

        var response = await http.PostAsync("online/oauth-login", new FormUrlEncodedContent(data), ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        var tokenResp = JsonSerializer.Deserialize<TokenResponse>(content);
        if (tokenResp is null || !tokenResp.Header.Success || string.IsNullOrWhiteSpace(tokenResp.ResponseObject.AccessToken))
            throw new Exception("Provider login failed");

        // если expires нет — верни null
        return (tokenResp.ResponseObject.AccessToken, null);
    }

    //private async Task AuthAsync(HttpClient http, string providerId, ProviderSettings settings, CancellationToken ct)
    //{
    //    var data = new Dictionary<string, string>
    //    {
    //        { "username", settings.User },
    //        { "password", settings.Password }
    //    };

    //    var content = new FormUrlEncodedContent(data);

    //    var response = await http.PostAsync("online/oauth-login", content);
    //    var responseContent = await response.Content.ReadAsStringAsync();

    //    TokenResponse? result = JsonSerializer.Deserialize<TokenResponse>(responseContent);
    //    if (result is null || !result.Header.Success)
    //        return;

    //    settings.Token = result.ResponseObject.AccessToken;

    //    using var scope = _scopeFactory.CreateScope();
    //    var providerRepo = scope.ServiceProvider.GetRequiredService<IProviderRepository>();
    //    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    //    var provider = (await providerRepo.GetAsync(providerId, ct))!;

    //    provider.UpdateSettings(JsonSerializer.Serialize(settings));

    //    await unitOfWork.CommitChangesAsync(ct);
    //}
}
