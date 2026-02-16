using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

[XmlType(Namespace = "urn:IPS-ws")]
public class Check3DCardRes
{
    public ResponseHeader? Header { get; set; }
    public Result? Result { get; set; }
}
