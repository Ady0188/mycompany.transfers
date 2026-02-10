using MyCompany.Transfers.Application.Common.Providers;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers.Dtos;
using NLog;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Helpers;
public static class Extensions
{
    static Logger _logger = LogManager.GetCurrentClassLogger();
    private static readonly Random _random = Random.Shared;

    private static string CanonicalizeXml(this string xmlContent)
    {
        XmlDocument doc = new XmlDocument();
        doc.PreserveWhitespace = true; // Important to preserve whitespace for canonicalization
        doc.LoadXml(xmlContent);

        // Create a canonicalization transform
        XmlDsigC14NTransform c14n = new XmlDsigC14NTransform();
        c14n.LoadInput(doc);

        // Get the canonicalized output
        Stream canonicalizedStream = (Stream)c14n.GetOutput(typeof(Stream));
        StreamReader reader = new StreamReader(canonicalizedStream);
        return reader.ReadToEnd();
    }

    public static (string CanonilizeXml, string Signature) GenerateSberSign(this string textSend, string userId, string key, string pass, string javaExecutablePath, string jarDirectory)
    {
        try
        {
            var xml = CanonicalizeXml(textSend).Replace("&#xD; ", "").Replace("&#xD;", "");

            if (string.IsNullOrEmpty(xml))
            {
                _logger.Debug("Empty data");
                return ("", "");
            }

            // Path to the JAR file
            //string jarDirectory = @"certificates\sber\signlibs";
            var javaExecExists = File.Exists(javaExecutablePath);
            var jarDirExists = Directory.Exists(jarDirectory);

            if (!javaExecExists || !jarDirExists)
            {
                _logger.Debug($"Jar executable file of jar dir is not exist. (exec file {javaExecExists}, jar dir {jarDirExists})");
                return ("", "");
            }

            var pathToFile = Path.Combine(jarDirectory, "sign-sandbox-1.0-SNAPSHOT.jar");
            if (!File.Exists(pathToFile))
            {
                _logger.Debug($"Jar file not found");
                return ("", "");
            }

            var runFileName = $"sign-sandbox-1.0-SNAPSHOT{Guid.NewGuid()}.jar";
            var runFile = Path.Combine(jarDirectory, runFileName);
            File.Copy(pathToFile, runFile);

            jarDirectory = new DirectoryInfo(jarDirectory).FullName.Replace("\\", "/");
            string classPath = $".\\{runFileName}";
            string mainClass = "tj.company.Main";

            string xmlArgument = Convert.ToBase64String(Encoding.Default.GetBytes(xml));

            // Arguments to pass to the JAR file, if any
            // Setting up the process start info
            ProcessStartInfo processStartInfo = new ProcessStartInfo(javaExecutablePath)
            {
                Arguments = $"-Dfile.encoding=UTF-8 -cp \"{Path.Combine(jarDirectory, classPath)}\" {mainClass} {xmlArgument} {key} {pass}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = jarDirectory
            };

            var cmdResult = string.Empty;

            // Starting the process
            using (Process process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();

                // Reading output to string
                cmdResult = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }

            File.Delete(runFile);

            var resLinses = cmdResult.Split("\r\n");

            if (resLinses.Length > 0 && resLinses[0].StartsWith("Error"))
            {
                _logger.Debug(resLinses[0]);
                return (resLinses[0], "");
            }
            else if (resLinses.Length < 2)
            {
                _logger.Debug("Generation sign error");
                return ("", "");
            }

            _logger.Debug("Generation sign success");

            return (resLinses[0], resLinses[1]);
        }
        catch (Exception ex)
        {
            _logger.Error($"Generation sign error: {ex}");
            return ("Error:", "");
        }
    }

    public static T? DeserializeXML<T>(this string input) where T : class
    {
        var serializer = new XmlSerializer(typeof(T));
        using var reader = new StringReader(input);
        return serializer.Deserialize(reader) as T;
    }

    public static T? Deserialize<T>(this string input)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<T>(input, options);
    }

    private static Dictionary<string, object?> GetReplacement(ProviderRequest r)
    {
        var dateTime = new DateTimeInfo(r.TransferDateTime);

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["TransferId"] = r.TransferId,
            ["NumId"] = r.NumId,
            ["ExternalId"] = r.ExternalId,
            ["ServiceId"] = r.ServiceId,
            ["Account"] = r.Account,
            ["CreditAmount"] = r.CreditAmount,
            ["CreditCurrency"] = new Currency(r.CurrencyIsoCode),
            ["Source"] = r.Source,
            ["DateTime"] = dateTime.Local,
            ["Proc"] = r.Proc,
            ["ProviderServicveId"] = r.ProviderServicveId,
            ["ProviderFee"] = r.ProviderFee
        };
    }

    private static void AddParam(Dictionary<string, object?> dict, string key, string? value)
    {
        if (key.StartsWith("sender_citizenship", StringComparison.OrdinalIgnoreCase))
        {
            dict["Param." + key] = new Country(value);
        }
        else if (key.StartsWith("sender_doc_issue_date", StringComparison.OrdinalIgnoreCase) || 
            key.StartsWith("sender_birth_date", StringComparison.OrdinalIgnoreCase))
        {
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
            {
                AddParam(dict, kv.Key, kv.Value);
            }
        }

        if (r.ProvReceivedParams is not null)
        {
            foreach (var kv in r.ProvReceivedParams)
            {
                AddParam(dict, kv.Key, kv.Value);
            }
        }

        return dict;
    }

    public static Dictionary<string, object?> BuildReplacements(this ProviderRequest r, Dictionary<string, string> parameters)
    {
        var dict = GetReplacement(r);

        if (parameters is not null)
        {
            foreach (var kv in parameters)
            {
                AddParam(dict, kv.Key, kv.Value);
            }
        }

        if (r.ProvReceivedParams is not null)
        {
            foreach (var kv in r.ProvReceivedParams)
            {
                AddParam(dict, kv.Key, kv.Value);
            }
        }

        return dict;
    }
    //public static Dictionary<string, string> BuildReplacements(this ProviderRequest r, Dictionary<string, string> parameters)
    //{
    //    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    //    {
    //        ["TransferId"] = r.TransferId,
    //        ["ExternalId"] = r.ExternalId ?? string.Empty,
    //        ["ServiceId"] = r.ServiceId,
    //        ["Account"] = r.Account ?? string.Empty,
    //        ["CreaditAmount"] = r.CreaditAmount.ToString(CultureInfo.InvariantCulture),
    //        ["CurrencyIsoCode"] = r.CurrencyIsoCode ?? string.Empty,
    //        ["Source"] = r.Source ?? string.Empty,
    //        ["Proc"] = r.Proc ?? string.Empty,
    //    };

    //    if (parameters is not null)
    //    {
    //        foreach (var kv in parameters)
    //        {
    //            // доступ к ним в шаблоне через [Param.Key]
    //            dict["Param." + kv.Key] = kv.Value ?? string.Empty;
    //        }
    //    }

    //    return dict;
    //}

    private static IReadOnlyList<string> ParseArgs(string? argsRaw)
    {
        if (string.IsNullOrWhiteSpace(argsRaw))
            return Array.Empty<string>();

        // простой парсер: разделение по запятым (без кавычек)
        // можно усложнить позже, если понадобятся кавычки и экранирование
        return argsRaw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    public static string ApplyTemplate(
    this string template,
    IReadOnlyDictionary<string, object?> values,
    bool encodeValues,
    Dictionary<string, string> additionals)
    {
        if (string.IsNullOrEmpty(template))
            return template;

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

                // [Guid] или [Guid:N]/[Guid:D]
                if (name.Equals("Guid", StringComparison.OrdinalIgnoreCase))
                {
                    tempResult = format is null
                        ? Guid.NewGuid().ToString("D")
                        : Guid.NewGuid().ToString(format);
                }
                // [Now] или [Now:yyyyMMdd]
                else if (name.Equals("Now", StringComparison.OrdinalIgnoreCase))
                {
                    var now = DateTime.UtcNow; // обычно лучше UTC для запросов/логов
                    tempResult = format is null
                        ? now.ToString("O", CultureInfo.InvariantCulture)
                        : now.ToString(format, CultureInfo.InvariantCulture);
                }
                // [Unix] или [Unix:ms]
                else if (name.Equals("Unix", StringComparison.OrdinalIgnoreCase))
                {
                    var dto = DateTimeOffset.UtcNow;
                    tempResult = (format?.Equals("ms", StringComparison.OrdinalIgnoreCase) ?? false)
                        ? dto.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)
                        : dto.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
                }
                // [Rnd:1000-9999] или [Rnd:hex:8]
                else if(name.Equals("Rnd", StringComparison.OrdinalIgnoreCase))
                {
                    if (format is null)
                        throw new FormatException("[Rnd] requires range, e.g. [Rnd:1-10]");

                    // hex
                    if (format.StartsWith("hex:", StringComparison.OrdinalIgnoreCase))
                    {
                        var lenPart = format.Substring("hex:".Length);
                        if (!int.TryParse(lenPart, out var hexLen) || hexLen <= 0)
                            throw new FormatException($"Invalid Rnd hex length: {format}");

                        // hexLen символов => нужно (hexLen+1)/2 байт
                        int bytesLen = (hexLen + 1) / 2;
                        Span<byte> bytes = bytesLen <= 256 ? stackalloc byte[bytesLen] : new byte[bytesLen];
                        RandomNumberGenerator.Fill(bytes);

                        // превращаем в hex и обрезаем до нужной длины
                        var hex = Convert.ToHexString(bytes); // uppercase
                        tempResult = hex.Substring(0, hexLen);
                    }
                    else
                    {
                        // диапазон min-max
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
                        _ => value.ToString()!
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
                var argsRaw = match.Groups["args"].Value.Split("|");

                if (!TemplateFunctions.Registry.TryGetValue(funcName, out var func))
                    throw new InvalidOperationException($"Unknown template function: {funcName}");

                // В твоём текущем синтаксисе функция получает 1 аргумент (готовая строка)
                var result = func(values, null, argsRaw, additionals);

                return encodeValues ? Uri.EscapeDataString(result) : result;
            });

        res = Regex.Replace(
            res,
            @"\[@(?<func>[A-Za-z0-9_.]+):(?<args>[^\]]*)\]",
            match =>
            {
                var funcName = match.Groups["func"].Value;
                var argsRaw = match.Groups["args"].Value.Split("|");

                if (!TemplateFunctions.Registry.TryGetValue(funcName, out var func))
                    throw new InvalidOperationException($"Unknown template function: {funcName}");

                // В твоём текущем синтаксисе функция получает 1 аргумент (готовая строка)
                var result = func(values, null, argsRaw, additionals);

                return encodeValues ? Uri.EscapeDataString(result) : result;
            });

        return res;
    }

    //public static string ApplyTemplate(this string template,
    //    IReadOnlyDictionary<string, string> values,
    //    bool encodeValues)
    //{
    //    if (string.IsNullOrEmpty(template))
    //        return template;

    //    return Regex.Replace(
    //        template,
    //        "\\[(?<name>[A-Za-z0-9_.]+)\\]",
    //        match =>
    //        {
    //            var name = match.Groups["name"].Value;

    //            if (!values.TryGetValue(name, out var value))
    //                return match.Value; // оставим как есть, чтобы не ломать шаблон

    //            if (encodeValues)
    //                return Uri.EscapeDataString(value);

    //            return value;
    //        });
    //}
}
