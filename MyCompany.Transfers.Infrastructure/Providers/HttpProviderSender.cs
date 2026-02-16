using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.XPath;

namespace MyCompany.Transfers.Infrastructure.Providers;

/// <summary>
/// Unified HTTP provider client. Sends requests and parses responses using provider configuration (Operations in SettingsJson).
/// Supports: Bearer/Basic auth, custom headers (HeaderTemplate), JSON/XML body and response,
/// dot-notation response paths (e.g. data.fullname), and status mapping (e.g. CHECKED -> SENDING).
/// Oracle and providers with non-HTTP or custom signing (e.g. Sber with Java JAR) remain separate clients.
/// </summary>
public sealed class HttpProviderSender : IProviderSender
{
    private readonly IHttpClientFactory _httpFactory;

    public HttpProviderSender(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    public async Task<ProviderResult> SendAsync(Provider p, ProviderRequest req, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("base");
        http.BaseAddress = new Uri(p.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(p.TimeoutSeconds);

        var settings = JsonSerializer.Deserialize<ProviderSettings>(p.SettingsJson)
                       ?? new ProviderSettings();

        if (!settings.Operations.TryGetValue(req.Operation, out var op))
        {
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                $"Operation '{req.Operation}' not configured for provider '{p.Id}'");
        }

        ApplyAuth(http, p);

        var replacements = req.BuildReplacements();

        var method = new HttpMethod(op.Method ?? "POST");
        var path = op.PathTemplate.ApplyTemplate(replacements, encodeValues: true, new Dictionary<string, string>());
        var request = new HttpRequestMessage(method, path);

        // Apply optional per-operation headers (e.g. serviceName, RqUID)
        ApplyHeaderTemplate(request, op, replacements);

        if (HttpMethodAllowsBody(method) && !string.IsNullOrWhiteSpace(op.BodyTemplate))
        {
            var bodyText = op.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

            if (string.Equals(op.Format, "xml", StringComparison.OrdinalIgnoreCase))
                request.Content = new StringContent(bodyText, Encoding.UTF8, "application/xml");
            else
                request.Content = new StringContent(bodyText, Encoding.UTF8, "application/json");
        }

        using var resp = await http.SendAsync(request, ct);

        if (!resp.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), $"HTTP {(int)resp.StatusCode}");

        var parsed = await ParseResponseAsync(resp, op, ct);

        // When status mapping is configured, map response value to OutboxStatus (e.g. TBank: CHECKED -> SENDING)
        if (!string.IsNullOrWhiteSpace(op.ResponseStatusPath) && op.StatusMapping is { Count: > 0 })
        {
            var statusValue = parsed.StatusValue ?? string.Empty;
            var statusStr = op.StatusMapping.TryGetValue(statusValue, out var mapped)
                ? mapped
                : op.StatusMapping.TryGetValue("*", out var def) ? def : null;

            if (!string.IsNullOrWhiteSpace(statusStr) && Enum.TryParse<OutboxStatus>(statusStr, true, out var mappedStatus))
            {
                var errMsg = (mappedStatus == OutboxStatus.FAILED || mappedStatus == OutboxStatus.FRAUD || mappedStatus == OutboxStatus.EXPIRED)
                    ? (parsed.ErrorMessage ?? parsed.ErrorValue ?? "Provider returned error")
                    : null;
                return new ProviderResult(mappedStatus, parsed.ResponseFieldValues, errMsg);
            }
        }

        if (!parsed.Success)
            return new ProviderResult(OutboxStatus.FAILED, parsed.ResponseFieldValues, parsed.ErrorMessage ?? "Provider returned error");

        return new ProviderResult(OutboxStatus.SUCCESS, parsed.ResponseFieldValues, null);
    }

    private static void ApplyHeaderTemplate(HttpRequestMessage request, ProviderOperationSettings op, IReadOnlyDictionary<string, object?> replacements)
    {
        if (op.HeaderTemplate is null || op.HeaderTemplate.Count == 0) return;
        var additionals = new Dictionary<string, string>();
        foreach (var (key, valueTemplate) in op.HeaderTemplate)
        {
            var value = valueTemplate.ApplyTemplate(replacements, encodeValues: false, additionals);
            request.Headers.TryAddWithoutValidation(key, value);
        }
    }

    private static async Task<ParsedResponse> ParseResponseAsync(
        HttpResponseMessage resp,
        ProviderOperationSettings op,
        CancellationToken ct)
    {
        if (string.Equals(op.ResponseFormat, "xml", StringComparison.OrdinalIgnoreCase))
            return await ParseXmlResponseAsync(resp, op, ct);
        return await ParseJsonResponseAsync(resp, op, ct);
    }

    private static async Task<ParsedResponse> ParseJsonResponseAsync(
        HttpResponseMessage resp,
        ProviderOperationSettings op,
        CancellationToken ct)
    {
        var raw = await resp.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(raw))
            return new ParsedResponse(false, new Dictionary<string, string>(), "Empty response", raw, null, null);

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        var responseFieldValues = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(op.ResponseField))
        {
            foreach (var item in op.ResponseField.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var path = item;
                var outputKey = item;
                var pipe = item.IndexOf('|');
                if (pipe >= 0)
                {
                    path = item[..pipe].Trim();
                    outputKey = item[(pipe + 1)..].Trim();
                }
                var value = GetJsonValue(root, path);
                if (value is not null)
                    responseFieldValues[outputKey] = value;
            }
        }

        string? statusValue = null;
        if (!string.IsNullOrWhiteSpace(op.ResponseStatusPath))
            statusValue = GetJsonValue(root, op.ResponseStatusPath);

        bool success = true;
        string? errorMessage = null;
        string? errorValue = null;

        if (!string.IsNullOrWhiteSpace(op.SuccessField))
        {
            var actual = GetJsonValue(root, op.SuccessField) ?? string.Empty;
            var expected = op.SuccessValue ?? "0";
            success = string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

            if (!success && !string.IsNullOrWhiteSpace(op.ErrorField))
            {
                errorValue = GetJsonValue(root, op.ErrorField);
                errorMessage = errorValue;
            }
        }

        return new ParsedResponse(success, responseFieldValues, errorMessage, raw, statusValue, errorValue);
    }

    /// <summary>
    /// Gets a string value from JSON by path (e.g. "data.fullname", "items[0].id").
    /// </summary>
    private static string? GetJsonValue(JsonElement root, string path)
    {
        var current = root;
        foreach (var rawSegment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var segment = rawSegment;
            int? index = null;
            var bracketIndex = segment.IndexOf('[');
            if (bracketIndex >= 0)
            {
                var propName = segment[..bracketIndex];
                var indexPart = segment[(bracketIndex + 1)..].TrimEnd(']');
                if (!int.TryParse(indexPart, out var idx))
                    return null;
                index = idx;
                segment = propName;
            }

            if (!current.TryGetProperty(segment, out current))
                return null;

            if (index.HasValue)
            {
                if (current.ValueKind != JsonValueKind.Array || index.Value >= current.GetArrayLength())
                    return null;
                current = current[index.Value];
            }
        }

        return JsonElementToString(current);
    }

    private static string? JsonElementToString(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => el.ToString(),
            JsonValueKind.Array => string.Join(",", el.EnumerateArray().Select(e => JsonElementToString(e) ?? "")),
            _ => el.GetRawText()
        };
    }

    private static async Task<ParsedResponse> ParseXmlResponseAsync(
        HttpResponseMessage resp,
        ProviderOperationSettings op,
        CancellationToken ct)
    {
        var xml = await resp.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(xml))
            return new ParsedResponse(false, new Dictionary<string, string>(), "Empty XML response", xml, null, null);

        var xdoc = XDocument.Parse(xml);
        var responseFieldValues = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(op.ResponseField))
        {
            foreach (var item in op.ResponseField.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var path = item;
                var outputKey = item;
                var pipe = item.IndexOf('|');
                if (pipe >= 0)
                {
                    path = item[..pipe].Trim();
                    outputKey = item[(pipe + 1)..].Trim();
                }
                var elem = xdoc.XPathSelectElement(path);
                if (elem is not null && !string.IsNullOrEmpty(elem.Value))
                    responseFieldValues[outputKey] = elem.Value;
            }
        }

        string? statusValue = null;
        if (!string.IsNullOrWhiteSpace(op.ResponseStatusPath))
        {
            var statusElem = xdoc.XPathSelectElement(op.ResponseStatusPath);
            statusValue = statusElem?.Value;
        }

        bool success = true;
        string? errorMessage = null;
        string? errorValue = null;

        if (!string.IsNullOrWhiteSpace(op.SuccessField))
        {
            var elem = xdoc.XPathSelectElement(op.SuccessField);
            var actual = elem?.Value ?? string.Empty;
            var expected = op.SuccessValue ?? "0";
            success = string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

            if (!success && !string.IsNullOrWhiteSpace(op.ErrorField))
            {
                var errElem = xdoc.XPathSelectElement(op.ErrorField);
                errorValue = errElem?.Value;
                errorMessage = errorValue;
            }
        }

        return new ParsedResponse(success, responseFieldValues, errorMessage, xml, statusValue, errorValue);
    }

    private static bool HttpMethodAllowsBody(HttpMethod method)
    {
        if (method == HttpMethod.Get || method == HttpMethod.Head)
            return false;
        return true;
    }

    private static void ApplyAuth(HttpClient http, Provider p)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(p.SettingsJson) ?? new();

        switch (p.AuthType)
        {
            case ProviderAuthType.Bearer:
                if (raw.TryGetValue("token", out var token))
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                break;

            case ProviderAuthType.Basic:
                if (raw.TryGetValue("user", out var u) &&
                    raw.TryGetValue("password", out var pwd))
                {
                    var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{u}:{pwd}"));
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64);
                }
                break;

            case ProviderAuthType.Hamac:
                http.DefaultRequestHeaders.Add(
                    raw.GetValueOrDefault("hmacHeader", "X-Signature"),
                    "<HMAC_SIGNATURE_PLACEHOLDER>");
                break;
        }
    }

    private sealed record ParsedResponse(
        bool Success,
        Dictionary<string, string> ResponseFieldValues,
        string? ErrorMessage,
        string? RawBody,
        string? StatusValue,
        string? ErrorValue);
}