using System.Xml.Serialization;

namespace MyCompany.Transfers.Contract.Tillabuy.Responses;

[XmlRoot(ElementName = "Response")]
public class GetRateResponse
{
    [XmlElement(ElementName = "Result")]
    public string Result { get; set; } = string.Empty;

    [XmlElement(ElementName = "ErrCode")]
    public int ErrCode { get; set; }

    [XmlElement(ElementName = "Rate")]
    public decimal Rate { get; set; }
}
