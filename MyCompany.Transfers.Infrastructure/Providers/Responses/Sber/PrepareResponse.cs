using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.Sber;

[XmlRoot("order")]
public class PrepareResponse
{
    [XmlElement("err")]
    public Error? Err { get; set; }

    public bool IsErr => Err != null;
}
