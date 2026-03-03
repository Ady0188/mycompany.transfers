using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

[XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class Check3DCardResponse
{
    [XmlElement(ElementName = "Body")]
    public Check3DCardBody? Body { get; set; }
}
