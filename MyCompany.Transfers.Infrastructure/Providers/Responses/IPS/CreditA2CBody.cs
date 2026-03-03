using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

public class CreditA2CBody
{
    [XmlElement(ElementName = "CreditA2CCipherRes", Namespace = "urn:IPS-ws")]
    public CreditA2CCipherRes? CreditA2CCipherRes { get; set; }
}
