using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

public class CreateStatus
{
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }
}
