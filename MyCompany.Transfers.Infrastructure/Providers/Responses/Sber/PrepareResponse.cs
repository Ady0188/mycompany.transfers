using MyCompany.Transfers.Domain.Providers;
using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.Sber;

[XmlRoot("order")]
public class PrepareResponse
{
    [XmlElement("version_protocol")]
    public string VersionProtocol { get; set; }

    [XmlElement("action")]
    public string Action { get; set; }

    [XmlElement("agent")]
    public string Agent { get; set; }

    [XmlElement("agent_date")]
    public DateTime AgentDate { get; set; }

    [XmlElement("operator")]
    public string Operator { get; set; }

    [XmlElement("act_type")]
    public string ActType { get; set; }

    [XmlElement("agentpaypoint")]
    public PrepareAgentPayPoint AgentPayPoint { get; set; }

    [XmlElement("operday_date")]
    public DateTime OperdayDate { get; set; }

    [XmlElement("pay_type")]
    public string PayType { get; set; }

    [XmlElement("nom_oper")]
    public long NomOper { get; set; }

    [XmlElement("summa")]
    public decimal Summa { get; set; }

    [XmlArray("services")]
    [XmlArrayItem("serv")]
    public List<PrepareService>? Services { get; set; }

    [XmlElement("commission")]
    public decimal Commission { get; set; }

    [XmlElement("unicum_code")]
    public string UnicumCode { get; set; }

    [XmlElement("suip")]
    public string Suip { get; set; }

    [XmlElement("err")]
    public Error? Err { get; set; }
    public bool IsErr => Err != null;
}
