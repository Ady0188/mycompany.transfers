using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MyCompany.Transfers.Application.Common.Providers;

namespace MyCompany.Transfers.Infrastructure.Helpers;

public static class ProviderRequestExtensions
{
    public static Dictionary<string, object?> BuildReplacements(this ProviderRequest r)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["TransferId"] = r.TransferId,
            ["NumId"] = r.NumId,
            ["ExternalId"] = r.ExternalId,
            ["ServiceId"] = r.ServiceId,
            ["ProviderServiceId"] = r.ProviderServiceId,
            ["Account"] = r.Account,
            ["CreditAmount"] = r.CreditAmount,
            ["ProviderFee"] = r.ProviderFee,
            ["CreditCurrency"] = r.CurrencyIsoCode,
            ["Source"] = r.Source,
            ["Proc"] = r.Proc,
            ["DateTime"] = r.TransferDateTime.ToString("O", CultureInfo.InvariantCulture)
        };

        if (r.Parameters is not null)
        {
            foreach (var kv in r.Parameters)
                dict["Param." + kv.Key] = kv.Value;
        }
        if (r.ProvReceivedParams is not null)
        {
            foreach (var kv in r.ProvReceivedParams)
                dict["Param." + kv.Key] = kv.Value;
        }
        return dict;
    }

    public static Dictionary<string, object?> BuildReplacements(this ProviderRequest r, Dictionary<string, string> extra)
    {
        var dict = r.BuildReplacements();
        foreach (var kv in extra)
            dict[kv.Key] = kv.Value;
        return dict;
    }

    public static string ApplyTemplate(
        this string template,
        IReadOnlyDictionary<string, object?> values,
        bool encodeValues,
        Dictionary<string, string> additionals)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        var additionalsSafe = additionals ?? new Dictionary<string, string>();

        var res = Regex.Replace(
            template,
            @"\[(?<name>[A-Za-z0-9_.]+)(:(?<format>[^\]]+))?\]",
            match =>
            {
                var name = match.Groups["name"].Value;
                var format = match.Groups["format"].Success
                    ? match.Groups["format"].Value
                    : null;

                string tempResult;

                if (name.Equals("Guid", StringComparison.OrdinalIgnoreCase))
                {
                    tempResult = format is null
                        ? Guid.NewGuid().ToString("D")
                        : Guid.NewGuid().ToString(format);
                }
                else if (name.Equals("Now", StringComparison.OrdinalIgnoreCase))
                {
                    var now = DateTime.UtcNow;
                    tempResult = format is null
                        ? now.ToString("O", CultureInfo.InvariantCulture)
                        : now.ToString(format, CultureInfo.InvariantCulture);
                }
                else if (name.Equals("Unix", StringComparison.OrdinalIgnoreCase))
                {
                    var dto = DateTimeOffset.UtcNow;
                    tempResult = (format?.Equals("ms", StringComparison.OrdinalIgnoreCase) ?? false)
                        ? dto.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
                        : dto.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
                }
                else if (name.Equals("Rnd", StringComparison.OrdinalIgnoreCase))
                {
                    if (format is null)
                        throw new FormatException("[Rnd] requires range, e.g. [Rnd:1-10]");

                    if (format.StartsWith("hex:", StringComparison.OrdinalIgnoreCase))
                    {
                        var lenPart = format.Substring("hex:".Length);
                        if (!int.TryParse(lenPart, out var hexLen) || hexLen <= 0)
                            throw new FormatException($"Invalid Rnd hex length: {format}");

                        int bytesLen = (hexLen + 1) / 2;
                        Span<byte> bytes = bytesLen <= 256 ? stackalloc byte[bytesLen] : new byte[bytesLen];
                        RandomNumberGenerator.Fill(bytes);

                        var hex = Convert.ToHexString(bytes);
                        tempResult = hex.Substring(0, hexLen);
                    }
                    else
                    {
                        var parts = format.Split('-', 2);
                        if (parts.Length != 2 ||
                            !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var min) ||
                            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var max))
                        {
                            throw new FormatException($"Invalid Rnd format: {format}");
                        }

                        if (min > max)
                            (min, max) = (max, min);

                        int value = Random.Shared.Next(min, max + 1);
                        int width = parts[1].Length;
                        tempResult = value.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
                    }
                }
                else
                {
                    if (!values.TryGetValue(name, out var value) || value is null)
                        return match.Value;

                    tempResult = value switch
                    {
                        IFormattable f when format != null
                            => f.ToString(format, CultureInfo.InvariantCulture),
                        IFormattable f
                            => f.ToString(null, CultureInfo.InvariantCulture),
                        _ => value.ToString() ?? ""
                    };
                }

                return encodeValues
                    ? Uri.EscapeDataString(tempResult)
                    : tempResult;
            });

        res = Regex.Replace(
            res,
            @"\[@@(?<func>[A-Za-z0-9_.]+):(?<args>[^\]]*)\]",
            match =>
            {
                var funcName = match.Groups["func"].Value;
                var argsRaw = match.Groups["args"].Value.Split('|');

                if (!TemplateFunctions.Registry.TryGetValue(funcName, out var func))
                    throw new InvalidOperationException($"Unknown template function: {funcName}");

                var result = func(values, null, argsRaw, additionalsSafe);
                return encodeValues ? Uri.EscapeDataString(result) : result;
            });

        res = Regex.Replace(
            res,
            @"\[@(?<func>[A-Za-z0-9_.]+):(?<args>[^\]]*)\]",
            match =>
            {
                var funcName = match.Groups["func"].Value;
                var argsRaw = match.Groups["args"].Value.Split('|');

                if (!TemplateFunctions.Registry.TryGetValue(funcName, out var func))
                    throw new InvalidOperationException($"Unknown template function: {funcName}");

                var result = func(values, null, argsRaw, additionalsSafe);
                return encodeValues ? Uri.EscapeDataString(result) : result;
            });

        return res;
    }
}
