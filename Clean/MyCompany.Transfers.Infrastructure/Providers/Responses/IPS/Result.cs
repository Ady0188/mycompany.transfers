using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

public class Result
{
    public int Code { get; set; }
    public string Description { get; set; } = string.Empty;
}
