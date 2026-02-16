using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

public class ResponseHeader
{
    public string Id { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}
