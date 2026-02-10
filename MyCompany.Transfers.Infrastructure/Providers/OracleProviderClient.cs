using Dapper;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Helpers;
using MyCompany.Transfers.Infrastructure.Repositories;
using NLog;
using System.Data;
using System.Text.Json;
using System.Xml.Linq;

namespace MyCompany.Transfers.Infrastructure.Providers;

internal sealed class OracleProviderClient : IProviderClient
{
    public string ProviderId => "IBT";
    private Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IDbOracleConnectionFactory _dbConnectionFactory;

    public OracleProviderClient(IDbOracleConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    //public async Task<ProviderCheckResult> CheckAsync(ProviderRequest request, CancellationToken ct)
    //{
    //    try
    //    {
    //        var result = await SendRequest(string.Empty, ct);

    //        if (result is null)
    //            return new ProviderCheckResult(false, null, null);



    //        return new ProviderCheckResult(true, new Dictionary<string, string>{{result, result}}, null);
    //    }
    //    catch (Exception ex)
    //    {
    //        return new ProviderCheckResult(false, null, ex.Message);
    //    }
    //}

    //public async Task<ProviderResult> SendAsync(ProviderRequest request, CancellationToken ct)
    //{
    //    try
    //    {
    //        var result = await SendRequest(string.Empty, ct);

    //        if (result is null)
    //            return new ProviderResult(false, null, null);
            
    //        return new ProviderResult(true, result, null);
    //    }
    //    catch (Exception ex)
    //    {
    //        return new ProviderResult(false, null, ex.Message);
    //    }
    //}

    public async Task<ProviderResult> SendAsync(Provider p, ProviderRequest r, CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<ProviderSettings>(p.SettingsJson)
                       ?? new ProviderSettings();

        if (!settings.Operations.TryGetValue(r.Operation, out var op))
        {
            return new ProviderResult(OutboxStatus.SETTING, new Dictionary<string, string>(),
                $"Operation '{r.Operation}' not configured for provider '{p.Id}'");
        }

        var replacements = r.BuildReplacements();

        var request = op.BodyTemplate!.ApplyTemplate(replacements, encodeValues: false, new Dictionary<string, string>());

        using var connection = await _dbConnectionFactory.CreateOracleConnectionAsync(cancellationToken);
        var parameters = new DynamicParameters();
        parameters.Add("strin", request, DbType.String, ParameterDirection.Input);
        parameters.Add("errcode", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("clobResult", dbType: DbType.String, direction: ParameterDirection.Output, size: int.MaxValue);

        // Выполнение запроса с использованием Dapper
        await connection.ExecuteAsync("company_mobile_banking.run_synch_query", parameters, commandType: CommandType.StoredProcedure);

        int errcode = parameters.Get<int>("errcode");
        string response = parameters.Get<string>("clobResult");

        response = response.Replace("<?xml version=\"1.0\"?>", "").Replace("OK_UTG", "OK").Trim().Replace("\n", "").Replace("\r", "");

        response = @"{""result"":0,""description"":""OK"",""fullname"":""Саидмирзоев Адаб Саидмирзоевич"",""currencies"":[""USD"",""EUR""]}";
        response = @"{""result"":0,""description"":""OK"",""data"":{""fullname"":""Фамилия Имя Отчество"",""currencies"":[""TJS"",""RUB"",""USD""]}}";
        if (string.IsNullOrWhiteSpace(response))
            return null;

        _logger.Debug($"Response Oracl client: {response} {request}");


        var parsed = await ParseResponseAsync(response, op, cancellationToken);

        if (!parsed.Success)
            return new ProviderResult(OutboxStatus.FAILED, parsed.ResponseFieldValues, parsed.ErrorMessage ?? "Provider returned error");


        return new ProviderResult(OutboxStatus.SUCCESS, parsed.ResponseFieldValues, parsed.ErrorMessage);
    }

    static Task<ParsedResponse> ParseResponseAsync(
    string resp,
    ProviderOperationSettings op,
    CancellationToken ct)
    {
        if (string.Equals(op.ResponseFormat, "xml", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ParseXmlResponse(resp, op));

        return Task.FromResult(ParseJsonResponse(resp, op));
    }

    static ParsedResponse ParseXmlResponse(
        string xml,
        ProviderOperationSettings op)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return new ParsedResponse(false, new Dictionary<string, string>(), "Empty XML response");

        var xdoc = XDocument.Parse(xml);
        var root = xdoc.Root ?? throw new InvalidOperationException("XML has no root element");

        var responseFieldValues = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(op.ResponseField))
        {
            foreach (var fieldPath in op.ResponseField.Split(
                         ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var value = GetXmlValue(root, fieldPath);
                if (value is not null)
                {
                    responseFieldValues[fieldPath] = value;
                }
            }
        }

        bool success = true;
        string? errorMessage = null;

        if (!string.IsNullOrWhiteSpace(op.SuccessField))
        {
            var actual = GetXmlValue(root, op.SuccessField) ?? string.Empty;
            var expected = op.SuccessValue ?? "0";

            success = string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

            if (!success && !string.IsNullOrWhiteSpace(op.ErrorField))
            {
                errorMessage = GetXmlValue(root, op.ErrorField);
            }
        }

        return new ParsedResponse(success, responseFieldValues, errorMessage);
    }

    static string? GetXmlValue(XElement root, string path)
    {
        XElement? current = root;

        foreach (var rawSegment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var segment = rawSegment;
            int? index = null;

            var bracketIndex = segment.IndexOf('[');
            if (bracketIndex >= 0)
            {
                var elemName = segment[..bracketIndex];
                var indexPart = segment[(bracketIndex + 1)..].TrimEnd(']');

                if (!int.TryParse(indexPart, out var idx))
                    return null;

                index = idx;
                segment = elemName;
            }

            var children = current.Elements(segment);

            if (index.HasValue)
            {
                current = children.Skip(index.Value).FirstOrDefault();
            }
            else
            {
                current = children.FirstOrDefault();
            }

            if (current is null)
                return null;
        }

        // Если последний элемент содержит несколько одноимённых детей, можно
        // сгладить их как массив (как в JSON).
        if (!current.HasElements)
            return current.Value;

        // пример: если унифицировать массивы:
        var values = current.Elements()
                            .Select(e => e.Value)
                            .ToArray();

        return values.Length > 0 ? string.Join(",", values) : current.Value;
    }

    //static async Task<ParsedResponse> ParseXmlResponseAsync(
    //    string xml,
    //    ProviderOperationSettings op,
    //    CancellationToken ct)
    //{
    //    if (string.IsNullOrWhiteSpace(xml))
    //        return new ParsedResponse(false, null, "Empty XML response");

    //    var xdoc = XDocument.Parse(xml);

    //    Dictionary<string, string> responseFieldValues = new();
    //    foreach (var field in op.ResponseField!.Split(","))
    //    {
    //        if (!string.IsNullOrWhiteSpace(field))
    //        {
    //            // field ожидаем как XPath: "/response/someField"
    //            var elem = xdoc.XPathSelectElement(field);
    //            if (elem is not null)
    //            {
    //                responseFieldValues[field] = elem.Value;
    //            }
    //        }
    //    }
    //    //string? opId = null;
    //    //if (!string.IsNullOrWhiteSpace(op.ResponseField))
    //    //{
    //    //    // ResponseField ожидаем как XPath: "/response/operationId"
    //    //    var elemOp = xdoc.XPathSelectElement(op.ResponseField);
    //    //    opId = elemOp?.Value;
    //    //}

    //    bool success = true;
    //    string? errorMessage = null;

    //    if (!string.IsNullOrWhiteSpace(op.SuccessField))
    //    {
    //        var elem = xdoc.XPathSelectElement(op.SuccessField);
    //        var actual = elem?.Value ?? string.Empty;
    //        var expected = op.SuccessValue ?? "0";

    //        success = string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

    //        if (!success && !string.IsNullOrWhiteSpace(op.ErrorField))
    //        {
    //            var errElem = xdoc.XPathSelectElement(op.ErrorField);
    //            errorMessage = errElem?.Value;
    //        }
    //    }

    //    return new ParsedResponse(success, responseFieldValues, errorMessage);
    //}

    static ParsedResponse ParseJsonResponse(
        string resp,
        ProviderOperationSettings op)
    {
        if (string.IsNullOrWhiteSpace(resp))
            return new ParsedResponse(false, null, "Empty response");

        using var doc = JsonDocument.Parse(resp);
        var root = doc.RootElement;

        var responseFieldValues = new Dictionary<string, string>();

        // читаем ResponseField как список путей через запятую
        if (!string.IsNullOrWhiteSpace(op.ResponseField))
        {
            foreach (var fieldPath in op.ResponseField.Split(
                         ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var value = GetJsonValue(root, fieldPath);
                if (value is not null)
                {
                    responseFieldValues[fieldPath] = value;
                }
            }
        }

        bool success = true;
        string? errorMessage = null;

        if (!string.IsNullOrWhiteSpace(op.SuccessField))
        {
            var actual = GetJsonValue(root, op.SuccessField) ?? string.Empty;
            var expected = op.SuccessValue ?? "0";

            success = string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);

            if (!success && !string.IsNullOrWhiteSpace(op.ErrorField))
            {
                errorMessage = GetJsonValue(root, op.ErrorField);
            }
        }

        return new ParsedResponse(success, responseFieldValues, errorMessage);
    }

    static string? GetJsonValue(JsonElement root, string path)
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
                if (current.ValueKind != JsonValueKind.Array ||
                    index.Value >= current.GetArrayLength())
                    return null;

                current = current[index.Value];
            }
        }

        return JsonElementToString(current);
    }

    static string? JsonElementToString(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;

            case JsonValueKind.String:
                return el.GetString();

            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                return el.ToString();

            case JsonValueKind.Array:
                {
                    var items = el.EnumerateArray()
                                  .Select(e => JsonElementToString(e) ?? string.Empty);
                    return string.Join(",", items); // "TJS,RUB,USD"
                }
            case JsonValueKind.Object:
            default:
                return el.GetRawText(); // на всякий случай — сырой JSON
        }
    }

    private sealed record ParsedResponse(
        bool Success,
        Dictionary<string, string> ResponseFieldValues,
        string? ErrorMessage);
}
