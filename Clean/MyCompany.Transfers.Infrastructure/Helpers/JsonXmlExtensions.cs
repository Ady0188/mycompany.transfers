using System.Text.Json;
using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Helpers;

public static class JsonXmlExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static T? Deserialize<T>(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return default;
        return JsonSerializer.Deserialize<T>(input, JsonOptions);
    }

    public static T? DeserializeXml<T>(this string input) where T : class
    {
        if (string.IsNullOrWhiteSpace(input)) return default;
        var serializer = new XmlSerializer(typeof(T));
        using var reader = new StringReader(input);
        return serializer.Deserialize(reader) as T;
    }
}
