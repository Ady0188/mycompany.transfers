using System.Xml.Serialization;

namespace MyCompany.Transfers.Contract.Tillabuy.Responses;

[XmlRoot(ElementName = "Response")]
public class GetBalanceResponse
{
    [XmlElement(ElementName = "Result")]
    public string Result { get; set; } = string.Empty;

    [XmlElement(ElementName = "ErrCode")]
    public int ErrCode { get; set; }

    [XmlElement(ElementName = "Balance")]
    public decimal Balance { get; set; }
}
