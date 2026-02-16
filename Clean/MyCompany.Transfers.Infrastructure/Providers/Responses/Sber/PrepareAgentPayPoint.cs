using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.Sber;

public class PrepareAgentPayPoint
{
    [XmlElement("pointcode")]
    public string PointCode { get; set; } = string.Empty;
}
