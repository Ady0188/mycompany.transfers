using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

public class StatusResponse
{
    [JsonPropertyName("header")]
    public Header Header { get; set; }

    [JsonPropertyName("responseObject")]
    public StatusResponseObject ResponseObject { get; set; }
}
