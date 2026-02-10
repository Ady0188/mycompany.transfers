using System.Xml.Serialization;

namespace MyCompany.Transfers.Contract.Tillabuy.Responses;

[XmlRoot("response")]
public class TestResponse
{
    [XmlElement("id")]
    public long Id { get; set; }

    [XmlElement("status")]
    public string Status { get; set; }
}