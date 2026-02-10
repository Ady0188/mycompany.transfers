using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;
using MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers;

internal sealed class IPSClient : IProviderClient
{
    public string ProviderId => "IPS";
    private Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly IServiceScopeFactory _scopeFactory;

    public IPSClient(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    private HttpClient SetHeaders(HttpClient http, ProviderOperationSettings settings, Dictionary<string, object?> replacements, Dictionary<string, string> additionals)
    {
        foreach (var item in settings.HeaderTemplate)
        {
            if (http.DefaultRequestHeaders.Contains(item.Key))
                http.DefaultRequestHeaders.Remove(item.Key);

            http.DefaultRequestHeaders.Add(item.Key, item.Value.ApplyTemplate(replacements, encodeValues: false, additionals));
        }

        return http;
    }

    public async Task<ProviderResult> SendAsync(Provider provider, ProviderRequest request, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var providerHttpHandlerCache = scope.ServiceProvider.GetRequiredService<IProviderHttpHandlerCache>();

        var settings = provider.SettingsJson.Deserialize<ProviderSettings>()
                       ?? new ProviderSettings();

        if (!settings.Operations.TryGetValue(request.Operation, out var op))
        {
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                $"Operation '{request.Operation}' not configured for provider '{provider.Id}'");
        }

        if (!settings.Common.TryGetValue("encryptKeyPath", out var encryptKeyPath) || string.IsNullOrWhiteSpace(encryptKeyPath))
        {
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                "IPS 'encryptKeyPath' does not exist");
        }

        var handler = providerHttpHandlerCache.GetOrCreate(provider.Id, settings);

        using var http = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri(provider.BaseUrl),
            Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds > 0 ? provider.TimeoutSeconds : 30)
        };

        var result = new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), null);
        if (request.Operation.ToLower() == "check")
            result = await CheckAsync(http, request, settings, encryptKeyPath, ct);
        else if (request.Operation.ToLower() == "credita2c")
            result = await CreditA2CAsync(http, request, settings, encryptKeyPath, ct);
        else if (request.Operation.ToLower() == "confirm")
            result = await ConfirmAsync(http, request, settings, encryptKeyPath, ct);

        return result;
    }

    private async Task<ProviderResult> SendXmlAsync<TResponse>(
    HttpClient http,
    ProviderRequest request,
    ProviderOperationSettings op,
    string encryptKeyPath,
    Func<TResponse, ProviderResult> mapResult,
    CancellationToken ct)
    where TResponse : class
    {
        var additionals = new Dictionary<string, string>
        {
            { "publicKeyPath", encryptKeyPath }
        };

        var replacements = request.BuildReplacements();

        var body = op.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, additionals);

        using var msg = new HttpRequestMessage(HttpMethod.Post, op.PathTemplate);

        if (op.HeaderTemplate != null)
        {
            foreach (var h in op.HeaderTemplate)
            {
                var value = h.Value.ApplyTemplate(replacements, encodeValues: false, additionals);
                msg.Headers.TryAddWithoutValidation(h.Key, value);
            }
        }

        msg.Content = new StringContent(body, Encoding.UTF8, "application/xml");

        using var resp = await http.SendAsync(msg, ct);

        var xml = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.Error($"IPS HTTP {(int)resp.StatusCode} {resp.StatusCode}. Body: {Trim(xml, 500)}");
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), resp.StatusCode.ToString());
        }

        TResponse? dto;
        try
        {
            var serializer = new XmlSerializer(typeof(TResponse));
            using var reader = new StringReader(xml);
            dto = serializer.Deserialize(reader) as TResponse;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"IPS invalid XML. Body: {Trim(xml, 500)}");
            return new ProviderResult(OutboxStatus.TECHNICAL, new Dictionary<string, string>(), "Invalid XML from provider");
        }

        if (dto == null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response from provider");

        try
        {
            return mapResult(dto);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "IPS response mapping failed");
            return new ProviderResult(OutboxStatus.TECHNICAL, new Dictionary<string, string>(), "Response mapping failed");
        }
    }

    private static string Trim(string? s, int maxLen)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Length <= maxLen ? s : s.Substring(0, maxLen) + "...";
    }

    private Task<ProviderResult> CheckAsync(
        HttpClient http,
        ProviderRequest request,
        ProviderSettings pSettings,
        string encryptKeyPath,
        CancellationToken ct)
    {
        var op = pSettings.Operations["check"];

        return SendXmlAsync<Check3DCardResponse>(
            http,
            request,
            op,
            encryptKeyPath,
            mapResult: r =>
            {
                var code = r.Body.Check3DCardRes.Result.Code;
                var desc = r.Body.Check3DCardRes.Result.Description;

                var status = code == 0 ? OutboxStatus.SUCCESS : OutboxStatus.FAILED;
                return new ProviderResult(status, new Dictionary<string, string>(), desc);
            },
            ct);
    }

    private async Task<ProviderResult> CheckAsync(HttpClient http, ProviderRequest request, string providerId, ProviderSettings pSettings, CancellationToken ct)
    {
        var settings = pSettings.Operations["check"];

        if (!pSettings.Common.TryGetValue("encryptKeyPath", out var encryptKey))
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), $"IPS 'encryptKeyPath' doues not exist");

        var additionals = new Dictionary<string, string>
        {
            { "publicKeyPath", pSettings.Common["encryptKeyPath"] }
        };


        var replacements = request.BuildReplacements();
        http = SetHeaders(http, settings, replacements, additionals);
        var body = settings.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, additionals);

        StringContent content = new StringContent(body, Encoding.UTF8, "application/xml");

        var response = await http.PostAsync(settings.PathTemplate, content);

        if (!response.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), response.StatusCode.ToString());

        var responseContent = await response.Content.ReadAsStringAsync();

        Check3DCardResponse? result;
        XmlSerializer serializer = new XmlSerializer(typeof(Check3DCardResponse));
        using StringReader reader = new StringReader(responseContent);
        {
            result = (Check3DCardResponse)serializer.Deserialize(reader);
        }

        if (result == null) 
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response from provider");

        var status = result.Body.Check3DCardRes.Result.Code == 0 ? OutboxStatus.SUCCESS : OutboxStatus.FAILED;
        return new ProviderResult(status, new Dictionary<string, string>(), result.Body.Check3DCardRes.Result.Description);
    }

    private Task<ProviderResult> CreditA2CAsync(
    HttpClient http,
    ProviderRequest request,
    ProviderSettings pSettings,
    string encryptKeyPath,
    CancellationToken ct)
    {
        var op = pSettings.Operations["credita2c"];

        return SendXmlAsync<CreditA2CResponse>(
            http,
            request,
            op,
            encryptKeyPath,
            mapResult: r =>
            {
                var code = r.Body.CreditA2CCipherRes.Result.Code;
                var desc = r.Body.CreditA2CCipherRes.Result.Description;

                var status = code == 0 ? OutboxStatus.SUCCESS : OutboxStatus.FAILED;
                return new ProviderResult(status, new Dictionary<string, string>(), desc);
            },
            ct);
    }

    private async Task<ProviderResult> CreditA2CAsync(HttpClient http, ProviderRequest request, string providerId, ProviderSettings pSettings, CancellationToken ct)
    {
        var settings = pSettings.Operations["credita2c"];

        if (!pSettings.Common.TryGetValue("encryptKeyPath", out var encryptKey))
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), $"IPS 'encryptKeyPath' doues not exist");

        var additionals = new Dictionary<string, string>
        {
            { "publicKeyPath", pSettings.Common["encryptKeyPath"] }
        };

        var replacements = request.BuildReplacements();
        http = SetHeaders(http, settings, replacements, additionals);
        var body = settings.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, additionals);

        StringContent content = new StringContent(body, Encoding.UTF8, "application/xml");

        var response = await http.PostAsync(settings.PathTemplate, content);

        if (!response.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), response.StatusCode.ToString());

        var responseContent = await response.Content.ReadAsStringAsync();

        CreditA2CResponse? result;
        XmlSerializer serializer = new XmlSerializer(typeof(CreditA2CResponse));
        using StringReader reader = new StringReader(responseContent);
        {
            result = (CreditA2CResponse)serializer.Deserialize(reader);
        }

        if (result == null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response from provider");

        var status = result.Body.CreditA2CCipherRes.Result.Code == 0 ? OutboxStatus.SUCCESS : OutboxStatus.FAILED;
        return new ProviderResult(status, new Dictionary<string, string>(), result.Body.CreditA2CCipherRes.Result.Description);
    }

    private Task<ProviderResult> ConfirmAsync(
    HttpClient http,
    ProviderRequest request,
    ProviderSettings pSettings,
    string encryptKeyPath,
    CancellationToken ct)
    {
        var op = pSettings.Operations["confirm"];

        return SendXmlAsync<ConfirmCreditResponse>(
            http,
            request,
            op,
            encryptKeyPath,
            mapResult: r =>
            {
                var code = r.Body.ConfirmCreditRes.Result.Code;
                var desc = r.Body.ConfirmCreditRes.Result.Description;

                var status = code == 0 ? OutboxStatus.SUCCESS : OutboxStatus.FAILED;
                return new ProviderResult(status, new Dictionary<string, string>(), desc);
            },
            ct);
    }

    private async Task<ProviderResult> ConfirmAsync(HttpClient http, ProviderRequest request, string providerId, ProviderSettings pSettings, CancellationToken ct)
    {
        var settings = pSettings.Operations["confirm"];

        if (!pSettings.Common.TryGetValue("encryptKeyPath", out var encryptKey))
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), $"IPS 'encryptKeyPath' doues not exist");

        var additionals = new Dictionary<string, string>
        {
            { "publicKeyPath", pSettings.Common["encryptKeyPath"] }
        };

        var replacements = request.BuildReplacements();
        http = SetHeaders(http, settings, replacements, additionals);
        var body = settings.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, additionals);

        StringContent content = new StringContent(body, Encoding.UTF8, "application/xml");

        var response = await http.PostAsync(settings.PathTemplate, content);

        if (!response.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), response.StatusCode.ToString());

        var responseContent = await response.Content.ReadAsStringAsync();

        ConfirmCreditResponse? result;
        XmlSerializer serializer = new XmlSerializer(typeof(ConfirmCreditResponse));
        using StringReader reader = new StringReader(responseContent);
        {
            result = (ConfirmCreditResponse)serializer.Deserialize(reader);
        }

        if (result == null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response from provider");

        var status = result.Body.ConfirmCreditRes.Result.Code == 0 ? OutboxStatus.SUCCESS : OutboxStatus.FAILED;
        return new ProviderResult(status, new Dictionary<string, string>(), result.Body.ConfirmCreditRes.Result.Description);
    }
}
