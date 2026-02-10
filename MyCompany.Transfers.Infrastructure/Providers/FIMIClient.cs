using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace MyCompany.Transfers.Infrastructure.Providers;

internal sealed class FIMIClient : IProviderClient
{
    public string ProviderId => "FIMI";
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private readonly IHttpClientFactory _httpFactory;
    private readonly IServiceScopeFactory _scopeFactory;

    private const string UnknownSessionMarker = "Unknown session";

    private static readonly HashSet<int> TransientCodes = new() { 408, 429, 500, 502, 503, 504 };

    public FIMIClient(IHttpClientFactory httpFactory, IServiceScopeFactory scopeFactory)
    {
        _httpFactory = httpFactory;
        _scopeFactory = scopeFactory;
    }

    public async Task<ProviderResult> SendAsync(Provider provider, ProviderRequest request, CancellationToken ct)
    {
        var pSettings = provider.SettingsJson.Deserialize<ProviderSettings>() ?? new ProviderSettings();

        if (!pSettings.Operations.TryGetValue(request.Operation, out var op))
        {
            return new ProviderResult(
                OutboxStatus.SETTING,
                new Dictionary<string, string>(),
                $"Operation '{request.Operation}' not configured for provider '{provider.Id}'");
        }

        var http = _httpFactory.CreateClient("base");
        http.BaseAddress = new Uri(provider.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds > 0 ? provider.TimeoutSeconds : 30);

        return request.Operation.Equals("posdeposit", StringComparison.OrdinalIgnoreCase)
            ? await PosDepositAsync(http, request, provider.Id, pSettings, ct)
            : new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(), $"Unsupported operation '{request.Operation}'");
    }

    private async Task<ProviderResult> PosDepositAsync(
        HttpClient http,
        ProviderRequest request,
        string providerId,
        ProviderSettings pSettings,
        CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IProviderTokenService>();

        var op = pSettings.Operations["posdeposit"];

        // 1) достаём sessionId
        var sessionId = await tokenService.GetAccessTokenAsync(providerId, ct);
        var dict = request.Parameters.ToDictionary();
        dict["sessionId"] = sessionId ?? "1";

        // 2) делаем запрос
        var body = BuildBody(request, op.BodyTemplate!, dict);
        var reply = await PostXmlWithRetryAsync(http, op.PathTemplate, body, request.ExternalId, ct);

        // 3) если unknown session — рефрешим и повторяем 1 раз
        if (IsUnknownSession(reply))
        {
            var newSession = await tokenService.RefreshOn401Async(
                providerId,
                loginFunc: innerCt => InitSessionAsync(http, request, pSettings, innerCt),
                ct);

            if (string.IsNullOrWhiteSpace(newSession))
            {
                _logger.Error($"Failed to initialize session for POS Replenishment {request.ExternalId}");
                return new ProviderResult(OutboxStatus.TECHNICAL, new Dictionary<string, string>(), "Failed to initialize session");
            }

            dict["sessionId"] = newSession;
            body = BuildBody(request, op.BodyTemplate!, dict);
            reply = await PostXmlWithRetryAsync(http, op.PathTemplate, body, request.ExternalId, ct);
        }

        // 4) SOAP Fault/processing error
        var fault = CheckProcessingErrorSafe(reply);
        if (fault.IsError)
        {
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), fault.Description);
        }

        // 5) нормальный разбор бизнес-ответа
        var parse = ConvertPosResponseSafe(reply);
        if (!parse.Success)
        {
            return new ProviderResult(OutboxStatus.TECHNICAL, new Dictionary<string, string>(), parse.Error);
        }

        var status = (parse.Result.Result == 1 && !string.IsNullOrWhiteSpace(parse.Result.ApprovalCode))
            ? OutboxStatus.SUCCESS
            : OutboxStatus.FAILED;

        // Можно сюда положить поля в ResponseFields при желании (ApprovalCode/TranId)
        return new ProviderResult(status, new Dictionary<string, string>(), status == OutboxStatus.SUCCESS ? null : "Declined/No approval code");
    }

    private static string BuildBody(ProviderRequest request, string template, Dictionary<string, string> dict)
    {
        var replacements = request.BuildReplacements(dict);
        return template.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());
    }

    private static bool IsUnknownSession(string reply) =>
        !string.IsNullOrEmpty(reply) &&
        reply.Contains("Unknown session", StringComparison.OrdinalIgnoreCase);

    private static TimeSpan Backoff(int attempt)
    {
        // 250, 500, 1000, 2000, 4000
        var ms = 250 * (1 << Math.Min(attempt - 1, 4));
        return TimeSpan.FromMilliseconds(ms);
    }

    private async Task<string> PostXmlWithRetryAsync(
        HttpClient http,
        string path,
        string xml,
        string? id,
        CancellationToken ct)
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

                // читаем body всегда (нужно для Unknown session и SOAP fault на 500)
                var reply = await resp.Content.ReadAsStringAsync(); // если доступно ReadAsStringAsync(ct) - используйте ct

                if (!string.IsNullOrEmpty(reply) && reply.Contains("version=\"1.1\""))
                {
                    reply = reply.Replace("version=\"1.1\"", "version=\"1.0\"")
                                 .Replace("&#x", "hex");
                }

                // Unknown session — возвращаем как маркер, даже если 500
                if (IsUnknownSession(reply))
                    return UnknownSessionMarker;

                if (resp.IsSuccessStatusCode)
                    return reply ?? string.Empty;

                var code = (int)resp.StatusCode;

                // non-transient: НЕ ретраим -> кидаем исключение (вы просили "ProviderResult либо исключение")
                if (!TransientCodes.Contains(code))
                {
                    throw new HttpRequestException($"Non-transient HTTP {(int)resp.StatusCode} {resp.StatusCode} (ID: {id})");
                }

                // transient: ретраи
                _logger.Warn($"Transient HTTP {(int)resp.StatusCode} {resp.StatusCode} (attempt {attempt}/{maxTries}) (ID: {id})");

                if (attempt == maxTries)
                    throw new HttpRequestException($"Transient HTTP {(int)resp.StatusCode} {resp.StatusCode} after retries (ID: {id})");

                await Task.Delay(Backoff(attempt), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                // сетевые ошибки сюда тоже попадают
                _logger.Warn(ex, $"HttpRequestException (attempt {attempt}/{maxTries}) (ID: {id})");

                if (attempt == maxTries)
                    throw;

                await Task.Delay(Backoff(attempt), ct);
            }
        }

        // по идее недостижимо
        throw new HttpRequestException($"Unknown HTTP failure (ID: {id})");
    }

    private async Task<(string accessToken, DateTimeOffset? expiresAtUtc)> InitSessionAsync(
        HttpClient http,
        ProviderRequest request,
        ProviderSettings pSettings,
        CancellationToken ct)
    {
        var op = pSettings.Operations["initsession"];
        var body = op.BodyTemplate!.ApplyTemplate(request.BuildReplacements(), encodeValues: false, new Dictionary<string, string>());

        const int maxRetries = 5;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var content = new StringContent(body, Encoding.UTF8, "text/xml");
                using var response = await http.PostAsync(op.PathTemplate, content, ct);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"InitSession HTTP {(int)response.StatusCode} {response.StatusCode}");

                var sessionId = GetSessionIdXmlSafe(responseContent);
                if (!string.IsNullOrWhiteSpace(sessionId))
                    return (sessionId, null);

                throw new Exception("Session ID not found in the response.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, $"InitSession attempt {attempt}/{maxRetries} failed");

                if (attempt == maxRetries)
                    throw;

                await Task.Delay(Backoff(attempt), ct);
            }
        }

        throw new Exception("InitSession failed");
    }

    // ---------- SAFE XML helpers ----------

    private static string GetSessionIdXmlSafe(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            return doc.Descendants().FirstOrDefault(d => d.Name.LocalName == "Id")?.Value ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private (bool IsError, string Description) CheckProcessingErrorSafe(string result)
    {
        if (string.IsNullOrEmpty(result)) return (true, "Empty response from provider");

        if (!result.Contains("<env:Fault>", StringComparison.OrdinalIgnoreCase))
            return (false, "");

        try
        {
            var doc = XDocument.Parse(result).Descendants();
            var textTag = doc.FirstOrDefault(d => d.Name.LocalName == "Text");
            var description = "KM Processing error.";

            if (result.Contains("Response=\"-11\"", StringComparison.OrdinalIgnoreCase))
                return (true, "Korti Milli is not available");

            if (textTag != null && !string.IsNullOrWhiteSpace(textTag.Value))
                description = "KM Processing error: " + textTag.Value;

            return (true, description);
        }
        catch
        {
            return (true, "KM Processing error (invalid fault XML)");
        }
    }

    private (bool Success, (string ApprovalCode, string AuthorizationNumber, int Result) Result, string Error)
        ConvertPosResponseSafe(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return (false, default, "Empty provider response");

        try
        {
            var nodes = XDocument.Parse(xml).Descendants();

            var approval = nodes.FirstOrDefault(d => d.Name.LocalName == "ApprovalCode")?.Value ?? string.Empty;
            var authResp = nodes.FirstOrDefault(d => d.Name.LocalName == "AuthRespCode")?.Value;
            var tranId = nodes.FirstOrDefault(d => d.Name.LocalName == "ThisTranId")?.Value ?? string.Empty;

            if (string.IsNullOrWhiteSpace(authResp))
                return (false, default, "Missing AuthRespCode in provider response");

            if (!int.TryParse(authResp, out var resultCode))
                return (false, default, $"Invalid AuthRespCode '{authResp}'");

            return (true, (approval, tranId, resultCode), "");
        }
        catch (Exception ex)
        {
            return (false, default, "Invalid XML response: " + ex.Message);
        }
    }
}

//internal sealed class FIMIClient : IProviderClient
//{
//    public string ProviderId => "FIMI";
//    private Logger _logger = LogManager.GetCurrentClassLogger();

//    private readonly IHttpClientFactory _httpFactory;
//    private readonly IServiceScopeFactory _scopeFactory;

//    public FIMIClient(IHttpClientFactory httpFactory, IServiceScopeFactory scopeFactory)
//    {
//        _httpFactory = httpFactory;
//        _scopeFactory = scopeFactory;
//    }

//    public async Task<ProviderResult> SendAsync(Provider provider, ProviderRequest request, CancellationToken ct)
//    {
//        var settings = provider.SettingsJson.Deserialize<ProviderSettings>()
//                       ?? new ProviderSettings();

//        var http = _httpFactory.CreateClient("base");
//        http.BaseAddress = new Uri(provider.BaseUrl);
//        http.Timeout = TimeSpan.FromSeconds(provider.TimeoutSeconds > 0 ? provider.TimeoutSeconds : 30);

//        if (!settings.Operations.TryGetValue(request.Operation, out var op))
//        {
//            return new ProviderResult(ProviderResultKind.Setting, new Dictionary<string, string>(),
//                $"Operation '{request.Operation}' not configured for provider '{provider.Id}'");
//        }

//        var result = new ProviderResult(ProviderResultKind.NoResponse, new Dictionary<string, string>(), null);
//        if (request.Operation.ToLower() == "posdeposit")
//                result = await PosDepositAsync(http, request, provider.Id, settings, ct);

//        return result;
//    }

//    public async Task<ProviderResult> PosDepositAsync(HttpClient http, ProviderRequest request, string providerId, ProviderSettings pSettings, CancellationToken ct)
//    {
//        using var scope = _scopeFactory.CreateScope();
//        var providerTokenService = scope.ServiceProvider.GetRequiredService<IProviderTokenService>();

//        var settings = pSettings.Operations["posdeposit"];

//        var sessionId = await providerTokenService.GetAccessTokenAsync(providerId, ct);
//        var dict = request.Parameters.ToDictionary();
//        dict.Add("sessionId", sessionId ?? "1");

//        var replacements = request.BuildReplacements(dict);
//        var body = settings.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

//        var result = await PostAsync(http, settings.PathTemplate, body, ct: ct);

//        if (result.Success && result.Result == "Unknown session")
//        {
//            sessionId = await providerTokenService.RefreshOn401Async(
//                providerId,
//                loginFunc: async innerCt =>
//                {
//                    return await InitSessionAsync(http, request, pSettings, innerCt);
//                },
//                ct);

//            if (string.IsNullOrEmpty(sessionId))
//            {
//                _logger.Error($"Failed to initialize session for POS Replenishment {request.ExternalId}");
//                return new ProviderResult(ProviderResultKind.Error, new Dictionary<string, string>(), null);
//            }

//            dict["sessionId"] = sessionId;

//            replacements = request.BuildReplacements(dict);
//            body = settings.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

//            result = await PostAsync(http, settings.PathTemplate, body, ct: ct);
//        }

//        if (result.Success && !CheckProcessingError(result.Result).IsError)
//        {
//            var res = ConvertPosResponse(result.Result);
//            var status = (res.Result == 1 && !string.IsNullOrWhiteSpace(res.ApprovalCode)) ? ProviderResultKind.Success : ProviderResultKind.Error;
//            return new ProviderResult(status, new Dictionary<string, string>(), "");
//        }

//        return new ProviderResult(ProviderResultKind.Error, new Dictionary<string, string>(), "");
//    }

//    private async Task<(string accessToken, DateTimeOffset? expiresAtUtc)> InitSessionAsync(
//        HttpClient http,
//        ProviderRequest request,
//        ProviderSettings pSettings,
//        CancellationToken ct)
//    {
//        var settings = pSettings.Operations["initsession"];

//        var replacements = request.BuildReplacements();
//        var body = settings.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

//        int tries = 0;
//        const int maxRetries = 5;

//        while (tries < maxRetries)
//        {
//            try
//            {
//                _logger.Info($"Sending InitSession request: {request}");

//                var content = new StringContent(body, Encoding.UTF8, "text/xml");

//                var response = await http.PostAsync(settings.PathTemplate, content, ct);
//                var responseContent = await response.Content.ReadAsStringAsync();

//                _logger.Info($"Received InitSession response: {responseContent}");

//                // Проверяем успешность запроса
//                if (!response.IsSuccessStatusCode)
//                {
//                    _logger.Error($"Request InitSession failed with status code {response.StatusCode}.");
//                    throw new Exception("InitSession failed");
//                }

//                // Парсим ответ
//                var sessionId = GetSessionIdXml(responseContent);

//                // Проверяем корректность полученного sessionId
//                if (!string.IsNullOrEmpty(sessionId))
//                {
//                    return (sessionId, null);
//                }

//                // Если sessionId не найден
//                _logger.Error("Session ID not found in the response.");
//                throw new Exception("Session ID not found in the response.");
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Exception while sending InitSession: {ex}");

//                tries++;
//                if (tries >= maxRetries)
//                {
//                    throw new Exception("InitSession failed");
//                }

//                // Задержка перед повторной попыткой
//                await Task.Delay(250 * tries, ct);
//            }
//        }

//        // Если все попытки не удались, возвращаем 0
//        throw new Exception("All attempts to send InitSession failed.");
//    }

//    private async Task<(bool Success, string Result)> PostAsync(HttpClient http, string path, string content, string? id = null, CancellationToken ct = default)
//    {
//        string reply = string.Empty;
//        int tries = 0;
//        int[] transientCodes = new int[] { 408, 429, 500, 502, 503, 504 };
//        while (tries < 5)
//        {
//            try
//            {
//                var request = new StringContent(content, Encoding.UTF8, "text/xml");

//                var response = await http.PostAsync(path, request, ct);

//                if (!response.IsSuccessStatusCode && transientCodes.Contains((int)response.StatusCode))
//                {
//                    _logger.Error($"HTTP request failed with status code {response.StatusCode} (ID: {id})");
//                    throw new HttpRequestException($"HTTP request failed with status code {response.StatusCode}");
//                }
//                else if (!response.IsSuccessStatusCode)
//                {
//                    _logger.Error($"HTTP request failed with status code {response.StatusCode} (ID: {id})");
//                    return (false, response.StatusCode.ToString());
//                }

//                reply = await response.Content.ReadAsStringAsync();

//                if (reply.Contains("version=\"1.1\""))
//                {
//                    reply = reply.Replace("version=\"1.1\"", "version=\"1.0\"")
//                                 .Replace("&#x", "hex");
//                }

//                if (reply.Contains("Unknown session #"))
//                {
//                    return (true, "Unknown session");
//                }

//                return (true, reply);
//            }
//            catch (HttpRequestException ex)
//            {
//                _logger.Error($"HttpRequestException: (ID: {id}) {ex.Message}");
//                tries++;
//                if (tries >= 5)
//                {
//                    throw;
//                }
//                await Task.Delay(250 * tries, ct);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error($"Unexpected error: (ID: {id}) {ex.Message}");
//                tries++;

//                if (tries >= 5)
//                {
//                    throw;
//                }

//                await Task.Delay(250 * tries, ct);
//            }
//        }

//        return (false, reply);
//    }


//    private string GetSessionIdXml(string session)
//    {
//        // Парсим XML и получаем Id
//        var doc = XDocument.Parse(session);

//        var code = doc.Descendants().FirstOrDefault(d => d.Name.LocalName == "Id");
//        return code != null ? code.Value : string.Empty;
//    }

//    private (bool IsError, string Description) CheckProcessingError(string result)
//    {
//        if (result.Contains("<env:Fault>"))
//        {
//            IEnumerable<XElement> doc = XDocument.Parse(result).Descendants();
//            var textTag = doc.FirstOrDefault(d => d.Name.LocalName == "Text");
//            var description = "KM Processing error. ";
//            if (textTag != null)
//            {
//                if (result.Contains("Response=\"-11\""))
//                {
//                    description = "Korti Milli is not available";
//                    return (true, description);
//                }
//                description = "KM Processing error: " + textTag.Value;
//            }

//            return (true, description);
//        }
//        return (false, "");
//    }

//    private (string ApprovalCode, string AuthorizationNumber, int Result) ConvertPosResponse(string processingResponse)
//    {
//        IEnumerable<XElement> doc = XDocument.Parse(processingResponse).Descendants();
//        var approvalCode = doc.FirstOrDefault(d => d.Name.LocalName == "ApprovalCode")?.Value ?? string.Empty;

//        var authRespCode = doc.First(d => d.Name.LocalName == "AuthRespCode").Value ?? string.Empty;
//        var tranId = doc.First(d => d.Name.LocalName == "ThisTranId").Value ?? string.Empty;
//        string description = string.Empty;
//        string requestId = string.Empty;

//        int result = int.Parse(authRespCode);

//        if (result == 1)
//        {
//            description = "OK";
//        }
//        else
//        {
//            description = "Unknown error";
//            var declineReason = doc.FirstOrDefault(d => d.Name.LocalName == "DeclineReason");
//            if (declineReason != null && !string.IsNullOrWhiteSpace(declineReason.Value))
//            {
//                description = declineReason.Value;
//            }
//        }

//        var code = doc.FirstOrDefault(d => d.Name.LocalName == "Response");
//        if (code != null && code.HasAttributes)
//        {
//            foreach (var item in code.Attributes())
//            {
//                if (item.Name == "TranId")
//                {
//                    requestId = item.Value;
//                }
//            }
//        }
//        return (approvalCode, tranId, result);
//    }

//}
