using MyCompany.Transfers.Domain.Providers;
using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.Sber;

[XmlRoot(ElementName = "order")]
public class ExecuteResponse
{

    [XmlElement(ElementName = "version_protocol")]
    public double VersionProtocol { get; set; }

    [XmlElement(ElementName = "action")]
    public string Action { get; set; }

    [XmlElement(ElementName = "agent")]
    public string Agent { get; set; }

    [XmlElement(ElementName = "agent_date")]
    public DateTime AgentDate { get; set; }

    [XmlElement(ElementName = "operator")]
    public string Operator { get; set; }

    [XmlElement(ElementName = "act_type")]
    public string ActType { get; set; }

    [XmlElement(ElementName = "agentpaypoint")]
    public PrepareAgentPayPoint Agentpaypoint { get; set; }

    [XmlElement(ElementName = "operday_date")]
    public DateTime OperdayDate { get; set; }

    [XmlElement(ElementName = "pay_type")]
    public string PayType { get; set; }

    [XmlElement(ElementName = "nom_oper")]
    public int NomOper { get; set; }

    [XmlElement(ElementName = "summa")]
    public double Summa { get; set; }

    [XmlElement(ElementName = "commission")]
    public double Commission { get; set; }

    [XmlElement(ElementName = "unicum_code")]
    public string UnicumCode { get; set; }

    [XmlElement(ElementName = "suip")]
    public string Suip { get; set; }

    [XmlElement(ElementName = "err")]
    public Error? Err { get; set; }
    public bool IsErr => Err != null;
}