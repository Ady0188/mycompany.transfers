using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

[XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class CreditA2CResponse
{
    [XmlElement(ElementName = "Body")]
    public CreditA2CBody? Body { get; set; }
}
