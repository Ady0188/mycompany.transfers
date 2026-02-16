using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

public class Check3DCardBody
{
    [XmlElement(ElementName = "Check3DCardRes", Namespace = "urn:IPS-ws")]
    public Check3DCardRes? Check3DCardRes { get; set; }
}
