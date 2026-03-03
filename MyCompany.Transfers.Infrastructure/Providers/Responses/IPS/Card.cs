using System.Xml.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.IPS;

public class Card
{
    public string PanMask { get; set; } = string.Empty;
    public string? ExpDate { get; set; }
    public string PaySys { get; set; } = string.Empty;
}
