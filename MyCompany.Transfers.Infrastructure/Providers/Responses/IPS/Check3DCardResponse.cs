using MyCompany.Transfers.Domain.Providers;
using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

[XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class Check3DCardResponse
{
    [XmlElement(ElementName = "Body")]
    public Check3DBody Body { get; set; }
}

public class Check3DBody
{
    [XmlElement(ElementName = "Check3DCardRes", Namespace = "urn:IPS-ws")]
    public Check3DCardRes Check3DCardRes { get; set; }
}

[XmlType(Namespace = "urn:IPS-ws")]
public class Check3DCardRes
{
    public ResponseHeader Header { get; set; }
    public Result Result { get; set; }
    public string CardEnrolled { get; set; }
    public Card Card { get; set; }
}

public class ResponseHeader
{
    public string Id { get; set; }
    public string Time { get; set; }
}

public class Result
{
    public int Code { get; set; }
    public string Description { get; set; }
}

public class Card
{
    public string PanMask { get; set; }
    public string? ExpDate { get; set; }
    public string PaySys { get; set; }
    public string Brand { get; set; }
    public string Product { get; set; }
    public string IssuerCountry { get; set; }
}