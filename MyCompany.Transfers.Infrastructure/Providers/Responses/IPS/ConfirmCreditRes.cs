using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

[XmlType(Namespace = "urn:IPS-ws")]
public class ConfirmCreditRes
{
    public Result? Result { get; set; }
}
