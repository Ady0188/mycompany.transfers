using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

[XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class ConfirmCreditResponse
{
    [XmlElement(ElementName = "Body")]
    public ConfirmCreditBody? Body { get; set; }
}
