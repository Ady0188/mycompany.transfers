using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;

namespace MyCompany.Transfers.Infrastructure.Providers;

public sealed class FIMIClient : IProviderClient
{
    public string ProviderId => "FIMI";
    private const string UnknownSessionMarker = "Unknown session";
    private static readonly HashSet<int> TransientCodes = [408, 429, 500, 502, 503, 504];

    private readonly IHttpClientFactory _httpFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FIMIClient> _logger;

    public FIMIClient(IHttpClientFactory httpFactory, IServiceScopeFactory scopeFactory, ILogger<FIMIClient> logger)
    {
        _httpFactory = httpFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<ProviderResult> SendAsync(Provider provider, ProviderRequest request, CancellationToken ct)
    {
        var pSettings = provider.SettingsJson.Deserialize<ProviderSettings>() ?? new ProviderSettings();
        if (!pSettings.Operations.TryGetValue(request.Operation, out var op))
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                $"Operation '{request.Operation}' not configured for provider '{provider.Id}'");

        var http = _httpFactory.CreateClient("base");
        http.BaseAddress = new Uri(provider.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds > 0 ? provider.TimeoutSeconds : 30);

        if (string.Equals(request.Operation, "posdeposit", StringComparison.OrdinalIgnoreCase))
            return await PosDepositAsync(http, request, provider.Id, pSettings, ct);

        return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
            $"Unsupported operation '{request.Operation}'");
    }

    private async Task<ProviderResult> PosDepositAsync(HttpClient http, ProviderRequest request, string providerId, ProviderSettings pSettings, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IProviderTokenService>();

        var op = pSettings.Operations["posdeposit"];
        var sessionId = await tokenService.GetAccessTokenAsync(providerId, ct);
        var dict = request.Parameters is not null
            ? new Dictionary<string, string>(request.Parameters, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        dict["sessionId"] = sessionId ?? "1";

        var body = BuildBody(request, op.BodyTemplate!, dict);
        var reply = await PostXmlWithRetryAsync(http, op.PathTemplate, body, request.ExternalId, ct);

        if (IsUnknownSession(reply))
        {
            var newSession = await tokenService.RefreshOn401Async(providerId,
                innerCt => InitSessionAsync(http, request, pSettings, innerCt), ct);
            if (string.IsNullOrWhiteSpace(newSession))
            {
                _logger.LogError("Failed to initialize session for POS {ExternalId}", request.ExternalId);
                return new ProviderResult(OutboxStatus.TECHNICAL, new Dictionary<string, string>(), "Failed to initialize session");
            }
            dict["sessionId"] = newSession;
            body = BuildBody(request, op.BodyTemplate!, dict);
            reply = await PostXmlWithRetryAsync(http, op.PathTemplate, body, request.ExternalId, ct);
        }

        var fault = CheckProcessingErrorSafe(reply);
        if (fault.IsError)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string> { ["errorCode"] = "PROCESSING_FAULT" }, fault.Description);

        var parse = ConvertPosResponseSafe(reply);
        if (!parse.Success)
            return new ProviderResult(OutboxStatus.TECHNICAL, new Dictionary<string, string>(), parse.Error);

        var resultDict = new Dictionary<string, string>();
        var status = (parse.Result.Result == 1 && !string.IsNullOrWhiteSpace(parse.Result.ApprovalCode))
            ? OutboxStatus.SUCCESS : OutboxStatus.FAILED;

        if (status == OutboxStatus.FAILED)
            resultDict["errorCode"] = parse.Result.Result.ToString();

        return new ProviderResult(status, resultDict,
            status == OutboxStatus.SUCCESS ? null : "Declined/No approval code");
    }

    private static string BuildBody(ProviderRequest request, string template, Dictionary<string, string> dict)
    {
        var replacements = request.BuildReplacements(dict);
        return template.ApplyTemplate(replacements, false, new Dictionary<string, string>());
    }

    private static bool IsUnknownSession(string reply) =>
        !string.IsNullOrEmpty(reply) && reply.Contains("Unknown session", StringComparison.OrdinalIgnoreCase);

    private static TimeSpan Backoff(int attempt)
    {
        var ms = 250 * (1 << Math.Min(attempt - 1, 4));
        return TimeSpan.FromMilliseconds(ms);
    }

    private async Task<string> PostXmlWithRetryAsync(HttpClient http, string path, string xml, string? id, CancellationToken ct)
    {
        const int maxTries = 5;
        for (int attempt = 1; attempt <= maxTries; attempt++)
        {
            try
            {
                using var msg = new HttpRequestMessage(HttpMethod.Post, path)
                {
                    Content = new StringContent(xml, Encoding.UTF8, "text/xml")
                };
                using var resp = await http.SendAsync(msg, ct);
                var reply = await resp.Content.ReadAsStringAsync(ct);

                if (!string.IsNullOrEmpty(reply) && reply.Contains("version=\"1.1\""))
                    reply = reply.Replace("version=\"1.1\"", "version=\"1.0\"").Replace("&#x", "hex");

                if (IsUnknownSession(reply)) return UnknownSessionMarker;
                if (resp.IsSuccessStatusCode) return reply ?? string.Empty;

                var code = (int)resp.StatusCode;
                if (!TransientCodes.Contains(code))
                    throw new HttpRequestException($"Non-transient HTTP {code} (ID: {id})");

                _logger.LogWarning("Transient HTTP {Code} attempt {Attempt}/{Max} (ID: {Id})", code, attempt, maxTries, id);
                if (attempt == maxTries)
                    throw new HttpRequestException($"Transient HTTP {code} after retries (ID: {id})");
                await Task.Delay(Backoff(attempt), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HttpRequestException attempt {Attempt}/{Max} (ID: {Id})", attempt, maxTries, id);
                if (attempt == maxTries) throw;
                await Task.Delay(Backoff(attempt), ct);
            }
        }
        throw new HttpRequestException($"Unknown HTTP failure (ID: {id})");
    }

    private async Task<(string accessToken, DateTimeOffset? expiresAtUtc)> InitSessionAsync(HttpClient http, ProviderRequest request, ProviderSettings pSettings, CancellationToken ct)
    {
        var op = pSettings.Operations["initsession"];
        var body = op.BodyTemplate!.ApplyTemplate(request.BuildReplacements(), false, new Dictionary<string, string>());
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                using var content = new StringContent(body, Encoding.UTF8, "text/xml");
                using var response = await http.PostAsync(op.PathTemplate, content, ct);
                var responseContent = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"InitSession HTTP {(int)response.StatusCode}");
                var sessionId = GetSessionIdXmlSafe(responseContent);
                if (!string.IsNullOrWhiteSpace(sessionId)) return (sessionId, null);
                throw new InvalidOperationException("Session ID not found in response");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "InitSession attempt {Attempt}/5 failed", attempt);
                if (attempt == 5) throw;
                await Task.Delay(Backoff(attempt), ct);
            }
        }
        throw new InvalidOperationException("InitSession failed");
    }

    private static string GetSessionIdXmlSafe(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            return doc.Descendants().FirstOrDefault(d => d.Name.LocalName == "Id")?.Value ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    private (bool IsError, string Description) CheckProcessingErrorSafe(string result)
    {
        if (string.IsNullOrEmpty(result)) return (true, "Empty response from provider");
        if (!result.Contains("<env:Fault>", StringComparison.OrdinalIgnoreCase)) return (false, "");

        try
        {
            if (result.Contains("Response=\"-11\"", StringComparison.OrdinalIgnoreCase))
                return (true, "Korti Milli is not available");
            var doc = XDocument.Parse(result).Descendants();
            var textTag = doc.FirstOrDefault(d => d.Name.LocalName == "Text");
            var description = textTag != null && !string.IsNullOrWhiteSpace(textTag.Value)
                ? "KM Processing error: " + textTag.Value : "KM Processing error.";
            return (true, description);
        }
        catch { return (true, "KM Processing error (invalid fault XML)"); }
    }

    private (bool Success, (string ApprovalCode, string AuthorizationNumber, int Result) Result, string Error) ConvertPosResponseSafe(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return (false, default, "Empty provider response");
        try
        {
            var nodes = XDocument.Parse(xml).Descendants();
            var approval = nodes.FirstOrDefault(d => d.Name.LocalName == "ApprovalCode")?.Value ?? string.Empty;
            var authResp = nodes.FirstOrDefault(d => d.Name.LocalName == "AuthRespCode")?.Value;
            var tranId = nodes.FirstOrDefault(d => d.Name.LocalName == "ThisTranId")?.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(authResp)) return (false, default, "Missing AuthRespCode");
            if (!int.TryParse(authResp, out var resultCode)) return (false, default, $"Invalid AuthRespCode '{authResp}'");
            return (true, (approval, tranId, resultCode), "");
        }
        catch (Exception ex) { return (false, default, "Invalid XML response: " + ex.Message); }
    }
}
