using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

public class TransferRespStatus
{
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("statusName")]
    public string StatusName { get; set; }

    [JsonPropertyName("statusDescription")]
    public string StatusDescription { get; set; }

    [JsonPropertyName("statusReasonMessageCode")]
    public string StatusReasonMessageCode { get; set; }

    [JsonPropertyName("statusReasonMessageDetail")]
    public string StatusReasonMessageDetail { get; set; }
}

