using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.Sber;

public class PrepareAddress
{
    [XmlElement("area")]
    public string Area { get; set; }

    [XmlElement("city")]
    public string City { get; set; }

    [XmlElement("corpus")]
    public string Corpus { get; set; }

    [XmlElement("house")]
    public string House { get; set; }

    [XmlElement("placename")]
    public string PlaceName { get; set; }

    [XmlElement("postalcode")]
    public string PostalCode { get; set; }

    [XmlElement("stateprov")]
    public string StateProv { get; set; }

    [XmlElement("street")]
    public string Street { get; set; }
}
