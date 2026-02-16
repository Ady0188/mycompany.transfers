using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

[XmlType(Namespace = "urn:IPS-ws")]
public class CreditA2CCipherRes
{
    public Result? Result { get; set; }
}
