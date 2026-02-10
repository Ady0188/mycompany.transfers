using MyCompany.Transfers.Domain.Providers;
using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

[XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class ConfirmCreditResponse
{
    [XmlElement(ElementName = "Body")]
    public ConfirmCreditBody Body { get; set; }
    public bool IsSuccess => Body.ConfirmCreditRes.Result.Code == 0;
}

public class ConfirmCreditBody
{
    [XmlElement(ElementName = "ConfirmCreditRes", Namespace = "urn:IPS-ws")]
    public ConfirmCreditRes ConfirmCreditRes { get; set; }
}

[XmlType(Namespace = "urn:IPS-ws")]
public class ConfirmCreditRes
{
    public ResponseHeader Header { get; set; }

    public Result Result { get; set; }

    public PaymentData PaymentData { get; set; }

    public Card Card { get; set; }
}

public class PaymentData
{
    public string ApprovalCode { get; set; }
    public string RRN { get; set; }
}