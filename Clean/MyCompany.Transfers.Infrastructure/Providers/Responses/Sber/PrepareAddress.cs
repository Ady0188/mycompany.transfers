using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.Sber;

public class PrepareAddress
{
    [XmlElement("street")]
    public string Street { get; set; } = string.Empty;

    [XmlElement("city")]
    public string City { get; set; } = string.Empty;
}
