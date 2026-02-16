using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;
using MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

namespace MyCompany.Transfers.Infrastructure.Providers;

public sealed class IPSClient : IProviderClient
{
    public string ProviderId => "IPS";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IPSClient> _logger;

    public IPSClient(IServiceScopeFactory scopeFactory, ILogger<IPSClient> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<ProviderResult> SendAsync(Provider provider, ProviderRequest request, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var handlerCache = scope.ServiceProvider.GetRequiredService<IProviderHttpHandlerCache>();
        var settings = provider.SettingsJson.Deserialize<ProviderSettings>() ?? new ProviderSettings();

        if (!settings.Operations.TryGetValue(request.Operation, out var op))
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                $"Operation '{request.Operation}' not configured for provider '{provider.Id}'");

        if (!settings.Common.TryGetValue("encryptKeyPath", out var encryptKeyPath) || string.IsNullOrWhiteSpace(encryptKeyPath))
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(), "IPS 'encryptKeyPath' not set");

        var handler = handlerCache.GetOrCreate(provider.Id, settings);
        using var http = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri(provider.BaseUrl),
            Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds > 0 ? provider.TimeoutSeconds : 30)
        };

        return request.Operation.ToLowerInvariant() switch
        {
            "check" => await SendXmlAsync<Check3DCardResponse>(http, request, op, encryptKeyPath,
                r => MapIPSResult(r.Body?.Check3DCardRes?.Result, OutboxStatus.SENDING, "check"), ct),
            "credita2c" => await SendXmlAsync<CreditA2CResponse>(http, request, op, encryptKeyPath,
                r => MapIPSResult(r.Body?.CreditA2CCipherRes?.Result, OutboxStatus.SENDING, "credita2c"), ct),
            "confirm" => await SendXmlAsync<ConfirmCreditResponse>(http, request, op, encryptKeyPath,
                r => MapIPSResult(r.Body?.ConfirmCreditRes?.Result, OutboxStatus.SUCCESS, "confirm"), ct),
            _ => new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                $"Unsupported operation '{request.Operation}'")
        };
    }

    private async Task<ProviderResult> SendXmlAsync<T>(HttpClient http, ProviderRequest request,
        ProviderOperationSettings op, string encryptKeyPath, Func<T, ProviderResult> mapResult, CancellationToken ct) where T : class
    {
        var additionals = new Dictionary<string, string> { ["publicKeyPath"] = encryptKeyPath };
        var replacements = request.BuildReplacements();
        var body = op.BodyTemplate!.ApplyTemplate(replacements, false, additionals);

        using var msg = new HttpRequestMessage(HttpMethod.Post, op.PathTemplate);
        if (op.HeaderTemplate is not null)
        {
            foreach (var (key, valueTemplate) in op.HeaderTemplate)
            {
                var value = valueTemplate.ApplyTemplate(replacements, false, additionals);
                msg.Headers.TryAddWithoutValidation(key, value);
            }
        }
        msg.Content = new StringContent(body, Encoding.UTF8, "application/xml");

        using var resp = await http.SendAsync(msg, ct);
        var xml = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("IPS HTTP {Code}. Body: {Body}", (int)resp.StatusCode, xml.Length > 500 ? xml[..500] + "..." : xml);
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string> { ["errorCode"] = ((int)resp.StatusCode).ToString() }, resp.StatusCode.ToString());
        }

        T? dto;
        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using var reader = new StringReader(xml);
            dto = serializer.Deserialize(reader) as T;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IPS invalid XML");
            return new ProviderResult(OutboxStatus.TECHNICAL, new Dictionary<string, string>(), "Invalid XML from provider");
        }

        if (dto is null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response");
        return mapResult(dto);
    }

    private static ProviderResult MapIPSResult(Result? result, OutboxStatus successStatus, string op)
    {
        var dict = new Dictionary<string, string>();
        var code = result?.Code ?? -1;
        var desc = result?.Description ?? "No description";
        if (code != 0)
        {
            dict["errorCode"] = code.ToString();
            return new ProviderResult(OutboxStatus.FAILED, dict, desc);
        }
        return new ProviderResult(successStatus, dict, null);
    }
}
