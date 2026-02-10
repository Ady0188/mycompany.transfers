using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.Sber;

public class Error
{
    [XmlElement("code")]
    public int Code { get; set; }

    [XmlElement("description")]
    public string Description { get; set; }
}