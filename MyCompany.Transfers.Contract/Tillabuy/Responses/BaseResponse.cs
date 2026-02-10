using System.Xml.Serialization;

namespace MyCompany.Transfers.Contract.Tillabuy.Responses;

[XmlRoot(ElementName = "Response")]
public class BaseResponse
{
    [XmlElement(ElementName = "Result")]
    public string Result { get; set; }
    [XmlElement(ElementName = "ErrCode")]
    public int ErrCode { get; set; }
    [XmlElement(ElementName = "Description")]
    public string Description { get; set; }
}