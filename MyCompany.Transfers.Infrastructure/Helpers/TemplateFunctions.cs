using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MyCompany.Transfers.Infrastructure.Helpers;

public delegate string TemplateFunc(
    IReadOnlyDictionary<string, object?> values,
    string? format,
    IReadOnlyList<string> args,
    Dictionary<string, string> additionalParams);

public static class TemplateFunctions
{
    public static readonly Dictionary<string, TemplateFunc> Registry =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["GetHashCode"] = GetAppHashCode,
            ["IPSEncryptData"] = IPSEncryptData,
            ["EncryptData"] = EncryptData,
            ["GetHash"] = GetHash,
            ["ComputeSha256Hash"] = ComputeSha256Hash,
            ["Signature"] = Signature,
            ["GetTBankSignature"] = GetTBankSignature,
            ["GetFIMITranCode"] = GetFIMITranCode,
            ["IfLenEq"] = IfLenEq,
            ["IfEq"] = IfEq,
            ["IfNotEq"] = IfNotEq,
            ["IfEmpty"] = IfEmpty,
            ["Substr"] = Substr,
            ["Left"] = Left,
            ["Right"] = Right,
            ["FromMinor"] = FromMinor,
        };

    private static string FromMinor(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        if (args.Count < 2)
            throw new FormatException("FromMinor expects: [@FromMinor:<minor>|<scale>|dot/comma]");

        var minorRaw = args[0];
        if (!long.TryParse(minorRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minor))
            throw new FormatException($"FromMinor minor must be integer, got: {minorRaw}");

        if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var scale) || scale < 0 || scale > 6)
            throw new FormatException($"FromMinor scale invalid: {args[1]}");

        var sepToken = (args.Count > 2 ? args[2] : "dot").Trim();

        decimal value = minor;
        for (int i = 0; i < scale; i++) value /= 10m;

        var s = value.ToString($"F{scale}", CultureInfo.InvariantCulture);

        if (sepToken.Equals("comma", StringComparison.OrdinalIgnoreCase) || sepToken == ",")
            s = s.Replace('.', ',');

        return s;
    }

    private static string Right(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        if (args.Count < 2)
            throw new FormatException("Right expects: [@Right:<text>,<count>]");

        var text = args[0] ?? string.Empty;

        if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
            throw new FormatException("Right count must be integer");

        if (string.IsNullOrEmpty(text) || count <= 0)
            return string.Empty;

        return text.Length <= count
            ? text
            : text.Substring(text.Length - count);
    }

    private static string Left(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        if (args.Count < 2)
            throw new FormatException("Left expects: [@Left:<text>,<count>]");

        var text = args[0] ?? string.Empty;

        if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
            throw new FormatException("Left count must be integer");

        if (string.IsNullOrEmpty(text) || count <= 0)
            return string.Empty;

        return text.Length <= count ? text : text.Substring(0, count);
    }

    private static string Substr(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        if (args.Count < 2)
            throw new FormatException("Substr expects: [@Substr:<text>,<start>[,<length>]]");

        var text = args[0] ?? string.Empty;

        if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var start))
            throw new FormatException("Substr start must be integer");

        int length = -1;
        if (args.Count > 2)
        {
            if (!int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out length))
                throw new FormatException("Substr length must be integer");
        }

        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (start < 0) start = 0;
        if (start >= text.Length) return string.Empty;

        if (length < 0 || start + length > text.Length)
            return text.Substring(start);

        return text.Substring(start, length);
    }

    private static string IfEmpty(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        var v = args.Count > 0 ? args[0] : string.Empty;
        var thenV = args.Count > 1 ? args[1] : string.Empty;
        var elseV = args.Count > 2 ? args[2] : string.Empty;

        return string.IsNullOrEmpty(v) ? thenV : elseV;
    }

    private static string IfLenEq(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        var s = args.Count > 0 ? args[0] : string.Empty;

        if (args.Count < 2 || !int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var len))
            throw new FormatException("IfLenEq expects: [@IfLenEq:<str>,<len>,<then>,<else>]");

        var thenValue = args.Count > 2 ? args[2] : string.Empty;
        var elseValue = args.Count > 3 ? args[3] : string.Empty;

        return (s?.Length == len) ? thenValue : elseValue;
    }

    private static string IfEq(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        var s = args.Count > 0 ? args[0] : string.Empty;
        var conditionValue = args.Count > 1 ? args[1] : string.Empty;

        if (string.IsNullOrEmpty(conditionValue))
            throw new FormatException("IfEq expects: [@IfEq:<str>,<conditionvalue>,<then>,<else>]");

        var thenValue = args.Count > 2 ? args[2] : string.Empty;
        var elseValue = args.Count > 3 ? args[3] : string.Empty;

        return (s == conditionValue) ? thenValue : elseValue;
    }

    private static string IfNotEq(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        var s = args.Count > 0 ? args[0] : string.Empty;
        var conditionValue = args.Count > 1 ? args[1] : string.Empty;

        if (string.IsNullOrEmpty(conditionValue))
            throw new FormatException("IfNotEq expects: [@IfEq:<str>,<conditionvalue>,<then>,<else>]");

        var thenValue = args.Count > 2 ? args[2] : string.Empty;
        var elseValue = args.Count > 3 ? args[3] : string.Empty;

        return (s == conditionValue) ? elseValue : thenValue;
    }

    private static string GetAppHashCode(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        var data = ResolveArg(args.ElementAtOrDefault(0), values);
        return Math.Abs(data.GetHashCode()).ToString();
    }

    private static string IPSEncryptData(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        var data = ResolveArg(args.ElementAtOrDefault(0), values);

        if (!additionalParams.TryGetValue("publicKeyPath", out var publicKeyPath))
            throw new ArgumentException("PublicKeyPath parameter is required for IPSEncryptData function.");

        string cryptoPackage = GenerateCryptoPackage(data, DateTime.UtcNow.ToString("yyyyMM"));
        byte[] messageBytes = HexStringToByteArray(cryptoPackage);
        byte[] publicKeyBytes = GetPublicKey(publicKeyPath);
        using RSA rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

        byte[] encrypted = rsa.Encrypt(messageBytes, RSAEncryptionPadding.Pkcs1);

        byte[] result = new byte[encrypted.Length + 1];
        result[0] = 0x11;
        Buffer.BlockCopy(encrypted, 0, result, 1, encrypted.Length);

        return Convert.ToBase64String(result);
    }

    private static string GenerateCryptoPackage(string pan, string? expDate)
    {
        string dataFormat = "000201";
        string panFormat = "0110" + pan;
        string expDateFormat = string.IsNullOrEmpty(expDate) ? "" : "0206" + expDate;
        string dateTimeFormat = "040E" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        return dataFormat + panFormat + expDateFormat + dateTimeFormat;
    }

    private static byte[] HexStringToByteArray(string hex)
    {
        int len = hex.Length;
        byte[] result = new byte[len / 2];
        for (int i = 0; i < len; i += 2)
            result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return result;
    }

    private static byte[] GetPublicKey(string publicKeyPath)
    {
        var keyBytes = File.ReadAllBytes(publicKeyPath);
        var publicKeyPem = Encoding.UTF8.GetString(keyBytes);
        return GetRsaPublicKeyFromBase64(publicKeyPem);
    }

    private static byte[] GetRsaPublicKeyFromBase64(string base64)
    {
        base64 = base64
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Trim();
        return Convert.FromBase64String(base64);
    }

    private static string EncryptData(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        var data = ResolveArg(args.ElementAtOrDefault(0), values);
        return data;
    }

    private static string GetHash(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        var algo = args.ElementAtOrDefault(0) ?? "SHA256";
        var data = ResolveArg(args.ElementAtOrDefault(1), values);
        return data;
    }

    private static string ComputeSha256Hash(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        var data = ResolveArg(args.ElementAtOrDefault(0), values);
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            builder.Append(b.ToString("x2"));
        return builder.ToString();
    }

    private static string GetTBankSignature(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        var data = ResolveArg(args.ElementAtOrDefault(0), values);
        var certPath = args.ElementAtOrDefault(1) ?? string.Empty;

        if (string.IsNullOrEmpty(data))
            return string.Empty;
        if (string.IsNullOrEmpty(certPath) || !File.Exists(certPath))
            return string.Empty;

        string pem = File.ReadAllText(certPath);
        using RSA rsa = RSA.Create();
        rsa.ImportFromPem(pem);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    private static string Signature(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        var algo = args.ElementAtOrDefault(0) ?? "HMACSHA256";
        var data = ResolveArg(args.ElementAtOrDefault(1), values);
        return data;
    }

    private static string GetFIMITranCode(IReadOnlyDictionary<string, object?> values, string? format, IReadOnlyList<string> args, Dictionary<string, string> additionalParams)
    {
        var data = ResolveArg(args.ElementAtOrDefault(0), values);
        string tranCode = "133";
        if (data.StartsWith("505827042") || data.StartsWith("505827043") || data.StartsWith("505827068"))
            tranCode = "140";
        return tranCode;
    }

    private static string ResolveArg(string? raw, IReadOnlyDictionary<string, object?> values)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        if (raw.StartsWith("[") && raw.EndsWith("]"))
        {
            var key = raw.Substring(1, raw.Length - 2);
            return values.TryGetValue(key, out var v) && v is not null ? v.ToString()! : string.Empty;
        }

        return raw;
    }
}
