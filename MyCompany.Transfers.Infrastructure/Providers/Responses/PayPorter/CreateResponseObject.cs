using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

public class CreateResponseObject
{
    [JsonPropertyName("transferOrderRefId")]
    public long TransferOrderRefId { get; set; }

    [JsonPropertyName("status")]
    public CreateStatus? Status { get; set; }
}
