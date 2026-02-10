using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.Sber;

public class PrepareService
{
    [XmlElement("serv_id")]
    public string ServId { get; set; }

    [XmlArray("pars")]
    [XmlArrayItem("par")]
    public List<PrepareParameter> Pars { get; set; }
}
