using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

public class StatusResponseObject
{
    [JsonPropertyName("transferStatus")]
    public TransferRespStatus TransferStatus { get; set; }
}