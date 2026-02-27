using MyCompany.Transfers.Application.Common.Providers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MyCompany.Transfers.Infrastructure.Helpers;

public static class ProviderRequestExtensions
{
    private static Dictionary<string, object?> GetReplacement(ProviderRequest r)
    {
        var dateTime = new DateTimeInfo(r.TransferDateTime);

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["TransferId"] = r.TransferId,
            ["NumId"] = r.NumId,
            ["ExternalId"] = r.ExternalId,
            ["ServiceId"] = r.ServiceId,
            ["ProviderServiceId"] = r.ProviderServiceId,
            ["Account"] = r.Account,
            ["CreditAmount"] = r.CreditAmount,
            ["CreditCurrency"] = new Currency(r.CurrencyIsoCode),
            ["Source"] = r.Source,
            ["DateTime"] = dateTime.Local,
            ["Proc"] = r.Proc,
            ["ProviderFee"] = r.ProviderFee
        };
    }

    private static void AddParam(Dictionary<string, object?> dict, string key, string? value)
    {
        if (key.StartsWith("sender_citizenship", StringComparison.OrdinalIgnoreCase))
        {
            dict["Param." + key] = new Country(value ?? string.Empty);
        }
        else if (key.StartsWith("sender_doc_issue_date", StringComparison.OrdinalIgnoreCase) ||
                 key.StartsWith("sender_birth_date", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(value))
                dict["Param." + key] = DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        else
        {
            dict["Param." + key] = value;
        }
    }

    public static Dictionary<string, object?> BuildReplacements(this ProviderRequest r)
    {
        var dict = GetReplacement(r);

        if (r.Parameters is not null)
        {
            foreach (var kv in r.Parameters)
                AddParam(dict, kv.Key, kv.Value);
        }

        if (r.ProvReceivedParams is not null)
        {
            foreach (var kv in r.ProvReceivedParams)
                AddParam(dict, kv.Key, kv.Value);
        }

        return dict;
    }

    public static Dictionary<string, object?> BuildReplacements(this ProviderRequest r, Dictionary<string, string> extra)
    {
        var dict = GetReplacement(r);

        if (extra is not null)
        {
            foreach (var kv in extra)
                AddParam(dict, kv.Key, kv.Value);
        }

        if (r.ProvReceivedParams is not null)
        {
            foreach (var kv in r.ProvReceivedParams)
                AddParam(dict, kv.Key, kv.Value);
        }

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
                else if (name.Equals("SberRqUID", StringComparison.OrdinalIgnoreCase))
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

    /// <summary>
    /// Локальное время/UTC для шаблонов провайдеров (как в оригинальном DateTimeInfo).
    /// </summary>
    private sealed class DateTimeInfo
    {
        public DateTimeInfo(DateTimeOffset utcDateTime, string timeZoneId = "Asia/Dushanbe")
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var localDateTime = TimeZoneInfo.ConvertTime(utcDateTime, tz);

            Utc = utcDateTime.ToUniversalTime().DateTime;
            Local = localDateTime.DateTime;
        }

        public DateTime Utc { get; }
        public DateTime Local { get; }
    }
}
