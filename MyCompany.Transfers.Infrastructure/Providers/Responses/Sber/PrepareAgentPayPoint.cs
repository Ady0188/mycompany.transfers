using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.Sber;

public class PrepareAgentPayPoint
{
    [XmlElement("pointcode")]
    public string PointCode { get; set; }

    [XmlElement("pointname")]
    public string PointName { get; set; }

    [XmlElement("pointaddress")]
    public PrepareAddress PointAddress { get; set; }
}
