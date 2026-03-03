using System.Text.Json;
using System.Xml.Linq;
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

    public static string ToXml(this Dictionary<string, object> dict, string rootName = "Root")
    {
        var root = new XElement(rootName);

        foreach (var kvp in dict)
        {
            root.Add(CreateElement(kvp.Key, kvp.Value));
        }

        return new XDocument(root).ToString();
    }

    private static XElement CreateElement(string key, object value)
    {
        if (value == null)
            return new XElement(key);

        if (value is Dictionary<string, object> nestedDict)
        {
            var element = new XElement(key);
            foreach (var nested in nestedDict)
            {
                element.Add(CreateElement(nested.Key, nested.Value));
            }
            return element;
        }

        return new XElement(key, value);
    }
}
