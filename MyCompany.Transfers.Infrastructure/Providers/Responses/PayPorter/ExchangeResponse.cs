using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

public class ExchangeResponse
{
    [JsonPropertyName("header")]
    public Header Header { get; set; } = null!;

    [JsonPropertyName("responseObject")]
    public ExchangeResponseObject ResponseObject { get; set; } = null!;
}
