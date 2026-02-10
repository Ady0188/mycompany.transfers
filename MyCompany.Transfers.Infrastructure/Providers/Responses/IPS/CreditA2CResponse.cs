using MyCompany.Transfers.Domain.Providers;
using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

[XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class CreditA2CResponse
{
    [XmlElement(ElementName = "Body")]
    public CreditA2CBody Body { get; set; }
}

public class CreditA2CBody
{
    [XmlElement(ElementName = "CreditA2CCipherRes", Namespace = "urn:IPS-ws")]
    public CreditA2CCipherRes CreditA2CCipherRes { get; set; }
}

[XmlType(Namespace = "urn:IPS-ws")]
public class CreditA2CCipherRes
{
    public ResponseHeader Header { get; set; }

    public Result Result { get; set; }

    public Card Card { get; set; }
}