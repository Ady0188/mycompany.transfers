using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

public class CreateResponse
{
    [JsonPropertyName("header")]
    public Header Header { get; set; } = null!;

    [JsonPropertyName("responseObject")]
    public CreateResponseObject? ResponseObject { get; set; }
}
