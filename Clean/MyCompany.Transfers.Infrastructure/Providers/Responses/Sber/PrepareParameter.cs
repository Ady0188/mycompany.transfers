using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.Sber;

public class PrepareParameter
{
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute("value")]
    public string Value { get; set; } = string.Empty;
}
