using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;
using MyCompany.Transfers.Infrastructure.Providers.Responses.Sber;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MyCompany.Transfers.Infrastructure.Providers;

internal class SberClient : IProviderClient
{
    public string ProviderId => "Sber";
    private Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpFactory;
    public SberClient(IServiceScopeFactory scopeFactory, IHttpClientFactory httpFactory)
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

        if (!settings.Operations.TryGetValue(request.Operation, out var op) && request.Operation.ToLower() != "confirm")
        {
            _logger.Warn($"Operation '{request.Operation}' not configured for provider '{provider.Id}'");
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                $"Operation '{request.Operation}' not configured for provider '{provider.Id}'");
        }

        if (!settings.Common.TryGetValue("agent", out var agent) || string.IsNullOrWhiteSpace(agent))
        {
            _logger.Warn("IPS 'agent' does not exist");
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                "IPS 'agent' does not exist");
        }

        if (!settings.Common.TryGetValue("encryptKeyPath", out var encryptKeyPath) || string.IsNullOrWhiteSpace(encryptKeyPath))
        {
            _logger.Warn("IPS 'encryptKeyPath' does not exist");
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                "IPS 'encryptKeyPath' does not exist");
        }

        if (!settings.Common.TryGetValue("encryptKeyPassword", out var encryptKeyPassword) || string.IsNullOrWhiteSpace(encryptKeyPassword))
        {
            _logger.Warn("IPS 'encryptKeyPassword' does not exist");
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                "IPS 'encryptKeyPassword' does not exist");
        }

        if (!settings.Common.TryGetValue("javaExecutablePath", out var javaExecutablePath) || string.IsNullOrWhiteSpace(javaExecutablePath))
        {
            _logger.Warn("IPS 'javaExecutablePath' does not exist");
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                "IPS 'javaExecutablePath' does not exist");
        }

        if (!settings.Common.TryGetValue("jarDirectory", out var jarDirectory) || string.IsNullOrWhiteSpace(jarDirectory))
        {
            _logger.Warn("IPS 'jarDirectory' does not exist");
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                "IPS 'jarDirectory' does not exist");
        }

        var http = _httpFactory.CreateClient("Sber");
        http.BaseAddress = new Uri(provider.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds > 0 ? provider.TimeoutSeconds : 30);

        //var handler = providerHttpHandlerCache.GetOrCreate(provider.Id, settings);

        //using var http = new HttpClient(handler, disposeHandler: false)
        //{
        //    BaseAddress = new Uri(provider.BaseUrl),
        //    Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds > 0 ? provider.TimeoutSeconds : 30)
        //};

        var result = new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), null);
        if (request.Operation.ToLower() == "prepare")
            result = await PrepareAsync(http, request, settings, encryptKeyPath, ct);
        else if (request.Operation.ToLower() == "execute" || request.Operation.ToLower() == "confirm")
            result = await ExecuteAsync(http, request, settings, encryptKeyPath, ct);

        return result;
    }

    private async Task<ProviderResult> PrepareAsync(
        HttpClient http,
        ProviderRequest request,
        ProviderSettings pSettings,
        string encryptKeyPath,
        CancellationToken ct)
    {
        var settings = pSettings.Operations["prepare"];

        var replacements = request.BuildReplacements();
        var body = settings.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

        var signResult = body.GenerateSberSign(pSettings.Common["agent"], 
            pSettings.Common["encryptKeyPath"], 
            pSettings.Common["encryptKeyPassword"], 
            pSettings.Common["javaExecutablePath"], 
            pSettings.Common["jarDirectory"]);

        if (http.DefaultRequestHeaders.Contains("Signature"))
            http.DefaultRequestHeaders.Remove("Signature");
        if (http.DefaultRequestHeaders.Contains("RqUID"))
            http.DefaultRequestHeaders.Remove("RqUID");

        http.DefaultRequestHeaders.Add("RqUID", GenerateRqUID());
        http.DefaultRequestHeaders.Add("Signature", signResult.Signature);

        _logger.Info($"Prepare Signature: {signResult.Signature}");
        _logger.Info($"Prepare Body: {signResult.CanonilizeXml}");

        StringContent content = new StringContent(signResult.CanonilizeXml, Encoding.UTF8, "application/xml");

        var response = await http.PostAsync(settings.PathTemplate, content);

        _logger.Info($"Response Status: {response.StatusCode}");

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.Info($"Response: {responseContent}");

        if (!response.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), response.StatusCode.ToString());
        
        var result = responseContent.DeserializeXML<PrepareResponse>();
        if (result == null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response from provider");

        var status = result.IsErr ? OutboxStatus.FAILED : OutboxStatus.SUCCESS;
        var description = result.IsErr ? result.Err.Description : "OK";
        return new ProviderResult(status, new Dictionary<string, string>(), description);
    }

    private async Task<ProviderResult> ExecuteAsync(
        HttpClient http,
        ProviderRequest request,
        ProviderSettings pSettings,
        string encryptKeyPath,
        CancellationToken ct)
    {
        var settings = pSettings.Operations["execute"];

        var replacements = request.BuildReplacements();
        var body = settings.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

        var signResult = body.GenerateSberSign(pSettings.Common["agent"],
            pSettings.Common["encryptKeyPath"],
            pSettings.Common["encryptKeyPassword"],
            pSettings.Common["javaExecutablePath"],
            pSettings.Common["jarDirectory"]);

        if (http.DefaultRequestHeaders.Contains("Signature"))
            http.DefaultRequestHeaders.Remove("Signature");

        http.DefaultRequestHeaders.Add("Signature", signResult.Signature);

        StringContent content = new StringContent(signResult.CanonilizeXml, Encoding.UTF8, "application/xml");

        var response = await http.PostAsync(settings.PathTemplate, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), response.StatusCode.ToString());

        var result = responseContent.DeserializeXML<PrepareResponse>();
        if (result == null)
            return new ProviderResult(OutboxStatus.NORESPONSE, new Dictionary<string, string>(), "Invalid response from provider");

        var status = result.IsErr ? OutboxStatus.FAILED : OutboxStatus.SUCCESS;
        var description = result.IsErr ? result.Err.Description : "OK";
        return new ProviderResult(status, new Dictionary<string, string>(), description);
    }

    private string GenerateRqUID()
    {
        // Create a 16-byte array to hold the random bytes
        var buffer = new byte[16];

        // Generate 16 random bytes using a cryptographically secure random number generator
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(buffer);
        }

        // Convert the bytes to a hexadecimal string
        var hex = new StringBuilder(32);
        foreach (var b in buffer)
        {
            hex.Append(b.ToString("x2")); // Format each byte as a 2-digit hexadecimal string
        }

        return hex.ToString();
    }
}