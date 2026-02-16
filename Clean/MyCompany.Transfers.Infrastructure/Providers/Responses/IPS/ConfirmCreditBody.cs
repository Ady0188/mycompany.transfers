using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

public class ConfirmCreditBody
{
    [XmlElement(ElementName = "ConfirmCreditRes", Namespace = "urn:IPS-ws")]
    public ConfirmCreditRes? ConfirmCreditRes { get; set; }
}
