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
public sealed class HttpProviderSender : IProviderSender
{
    private readonly IHttpClientFactory _httpFactory;

    public HttpProviderSender(IHttpClientFactory httpFactory) => _httpFactory = httpFactory;

    public async Task<ProviderResult> SendAsync(Provider p, ProviderRequest req, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("base");
        http.BaseAddress = new Uri(p.BaseUrl);
        http.Timeout = TimeSpan.FromSeconds(p.TimeoutSeconds);

        // 1. Разбираем настройки провайдера
        var settings = JsonSerializer.Deserialize<ProviderSettings>(p.SettingsJson)
                       ?? new ProviderSettings();

        if (!settings.Operations.TryGetValue(req.Operation, out var op))
        {
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                $"Operation '{req.Operation}' not configured for provider '{p.Id}'");
        }

        // 2. Аутентификация (Bearer/Basic/Hmac)
        ApplyAuth(http, p);

        // 3. Готовим словарь значений для подстановки
        var replacements = req.BuildReplacements();

        // 4. Собираем HTTP-запрос
        var method = new HttpMethod(op.Method ?? "POST");
        var path = op.PathTemplate.ApplyTemplate(replacements, encodeValues: true, new Dictionary<string, string>()); // GET query -> URL-encode
        var request = new HttpRequestMessage(method, path);

        // 5. Тело: только если метод позволяет и шаблон есть
        if (HttpMethodAllowsBody(method) && !string.IsNullOrWhiteSpace(op.BodyTemplate))
        {
            var bodyText = op.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

            if (string.Equals(op.Format, "xml", StringComparison.OrdinalIgnoreCase))
            {
                request.Content = new StringContent(bodyText, Encoding.UTF8, "application/xml");
            }
            else // json по умолчанию
            {
                request.Content = new StringContent(bodyText, Encoding.UTF8, "application/json");
            }
        }

        // 6. Отправляем
        using var resp = await http.SendAsync(request, ct);

        if (!resp.IsSuccessStatusCode)
            return new ProviderResult(OutboxStatus.FAILED, new Dictionary<string, string>(), $"HTTP {(int)resp.StatusCode}");

        var parsed = await ParseResponseAsync(resp, op, ct);

        if (!parsed.Success)
            return new ProviderResult(OutboxStatus.FAILED, parsed.ResponseFieldValues, parsed.ErrorMessage ?? "Provider returned error");

        return new ProviderResult(OutboxStatus.SUCCESS, parsed.ResponseFieldValues, null);
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
        var dict = await resp.Content.ReadFromJsonAsync<Dictionary<string, object?>>(cancellationToken: ct);
        if (dict is null)
            return new ParsedResponse(false, null, "Empty response");

        Dictionary<string, string> responseFieldValues = new();

        foreach (var field in op.ResponseField!.Split(","))
        {
            if (!string.IsNullOrWhiteSpace(field) &&
            dict.TryGetValue(field, out var fieldVal) &&
            fieldVal is not null)
            {
                responseFieldValues[field] = fieldVal.ToString();
            }
        }

        //string? opId = null;
        //if (!string.IsNullOrWhiteSpace(op.ResponseField) &&
        //    dict.TryGetValue(op.ResponseField, out var opVal) &&
        //    opVal is not null)
        //{
        //    opId = opVal.ToString();
        //}

        bool success = true;
        string? errorMessage = null;

        if (!string.IsNullOrWhiteSpace(op.SuccessField))
        {
            dict.TryGetValue(op.SuccessField, out var successValObj);
            var actual = successValObj?.ToString() ?? string.Empty;
            var expected = op.SuccessValue ?? "0";

            success = string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

            if (!success && !string.IsNullOrWhiteSpace(op.ErrorField))
            {
                if (dict.TryGetValue(op.ErrorField, out var errObj) && errObj is not null)
                    errorMessage = errObj.ToString();
            }
        }

        return new ParsedResponse(success, responseFieldValues, errorMessage);
    }

    private static async Task<ParsedResponse> ParseXmlResponseAsync(
    HttpResponseMessage resp,
    ProviderOperationSettings op,
    CancellationToken ct)
    {
        var xml = await resp.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(xml))
            return new ParsedResponse(false, null, "Empty XML response");

        var xdoc = XDocument.Parse(xml);

        Dictionary<string, string> responseFieldValues = new();
        foreach (var field in op.ResponseField!.Split(","))
        {
            if (!string.IsNullOrWhiteSpace(field))
            {
                // field ожидаем как XPath: "/response/someField"
                var elem = xdoc.XPathSelectElement(field);
                if (elem is not null)
                {
                    responseFieldValues[field] = elem.Value;
                }
            }
        }
        
        //string? opId = null;
        //if (!string.IsNullOrWhiteSpace(op.ResponseField))
        //{
        //    // ResponseField ожидаем как XPath: "/response/operationId"
        //    var elemOp = xdoc.XPathSelectElement(op.ResponseField);
        //    opId = elemOp?.Value;
        //}

        bool success = true;
        string? errorMessage = null;

        if (!string.IsNullOrWhiteSpace(op.SuccessField))
        {
            var elem = xdoc.XPathSelectElement(op.SuccessField);
            var actual = elem?.Value ?? string.Empty;
            var expected = op.SuccessValue ?? "0";

            success = string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

            if (!success && !string.IsNullOrWhiteSpace(op.ErrorField))
            {
                var errElem = xdoc.XPathSelectElement(op.ErrorField);
                errorMessage = errElem?.Value;
            }
        }

        return new ParsedResponse(success, responseFieldValues, errorMessage);
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
        string? ErrorMessage);
}