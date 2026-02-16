using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;
using MyCompany.Transfers.Infrastructure.Providers.Responses.Sber;

namespace MyCompany.Transfers.Infrastructure.Providers;

public sealed class SberClient : IProviderClient
{
    public string ProviderId => "Sber";
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SberClient> _logger;

    public SberClient(IHttpClientFactory httpFactory, ILogger<SberClient> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<ProviderResult> SendAsync(Provider provider, ProviderRequest request, CancellationToken ct)
    {
        var settings = provider.SettingsJson.Deserialize<ProviderSettings>() ?? new ProviderSettings();

        if (!settings.Operations.TryGetValue(request.Operation, out var op) && !string.Equals(request.Operation, "confirm", StringComparison.OrdinalIgnoreCase))
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                $"Operation '{request.Operation}' not configured for provider '{provider.Id}'");

        foreach (var key in new[] { "agent", "encryptKeyPath", "encryptKeyPassword", "javaExecutablePath", "jarDirectory" })
        {
            if (!settings.Common.TryGetValue(key, out var val) || string.IsNullOrWhiteSpace(val))
                return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(), $"Sber '{key}' not set");
        }

        var http = _httpFactory.CreateClient("Sber");
        http.BaseAddress = new Uri(provider.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds > 0 ? provider.TimeoutSeconds : 30);

        if (string.Equals(request.Operation, "prepare", StringComparison.OrdinalIgnoreCase))
            return await PrepareAsync(http, request, settings, ct);
        if (string.Equals(request.Operation, "execute", StringComparison.OrdinalIgnoreCase) || string.Equals(request.Operation, "confirm", StringComparison.OrdinalIgnoreCase))
            return await ExecuteAsync(http, request, settings, ct);

        return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
            $"Unsupported operation '{request.Operation}'");
    }

    private async Task<ProviderResult> PrepareAsync(HttpClient http, ProviderRequest request, ProviderSettings pSettings, CancellationToken ct)
    {
        var op = pSettings.Operations["prepare"];
        var replacements = request.BuildReplacements();
        var body = op.BodyTemplate!.ApplyTemplate(replacements, false, new Dictionary<string, string>());

        var (canonicalXml, signature) = body.GenerateSberSign(
            pSettings.Common["agent"],
            pSettings.Common["encryptKeyPath"],
            pSettings.Common["encryptKeyPassword"],
            pSettings.Common["javaExecutablePath"],
            pSettings.Common["jarDirectory"],
            _logger);

        if (string.IsNullOrEmpty(signature))
            return new ProviderResult(OutboxStatus.TECHNICAL, new Dictionary<string, string>(), "Sber sign generation failed");

        http.DefaultRequestHeaders.Remove("Signature");
        http.DefaultRequestHeaders.Remove("RqUID");
        http.DefaultRequestHeaders.Add("RqUID", GenerateRqUID());
        http.DefaultRequestHeaders.Add("Signature", signature);

        var content = new StringContent(canonicalXml, Encoding.UTF8, "application/xml");
        var response = await http.PostAsync(op.PathTemplate, content, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string> { ["errorCode"] = ((int)response.StatusCode).ToString() }, response.StatusCode.ToString());

        var result = responseContent.DeserializeXml<PrepareResponse>();
        if (result is null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response");

        var dict = new Dictionary<string, string>();
        if (result.IsErr)
        {
            dict["errorCode"] = result.Err?.Code.ToString() ?? "ERR";
            return new ProviderResult(OutboxStatus.FAILED, dict, result.Err?.Description ?? "Error");
        }
        return new ProviderResult(OutboxStatus.SENDING, dict, null);
    }

    private async Task<ProviderResult> ExecuteAsync(HttpClient http, ProviderRequest request, ProviderSettings pSettings, CancellationToken ct)
    {
        var op = pSettings.Operations["execute"];
        var replacements = request.BuildReplacements();
        var body = op.BodyTemplate!.ApplyTemplate(replacements, false, new Dictionary<string, string>());

        var (canonicalXml, signature) = body.GenerateSberSign(
            pSettings.Common["agent"],
            pSettings.Common["encryptKeyPath"],
            pSettings.Common["encryptKeyPassword"],
            pSettings.Common["javaExecutablePath"],
            pSettings.Common["jarDirectory"],
            _logger);

        if (string.IsNullOrEmpty(signature))
            return new ProviderResult(OutboxStatus.TECHNICAL, new Dictionary<string, string>(), "Sber sign generation failed");

        http.DefaultRequestHeaders.Remove("Signature");
        http.DefaultRequestHeaders.Add("Signature", signature);

        var content = new StringContent(canonicalXml, Encoding.UTF8, "application/xml");
        var response = await http.PostAsync(op.PathTemplate, content, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string> { ["errorCode"] = ((int)response.StatusCode).ToString() }, response.StatusCode.ToString());

        var result = responseContent.DeserializeXml<PrepareResponse>();
        if (result is null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response");

        var dict = new Dictionary<string, string>();
        if (result.IsErr)
        {
            dict["errorCode"] = result.Err?.Code.ToString() ?? "ERR";
            return new ProviderResult(OutboxStatus.FAILED, dict, result.Err?.Description ?? "Error");
        }
        return new ProviderResult(OutboxStatus.SUCCESS, dict, null);
    }

    private static string GenerateRqUID()
    {
        var buffer = new byte[16];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToHexString(buffer).ToLowerInvariant();
    }
}
