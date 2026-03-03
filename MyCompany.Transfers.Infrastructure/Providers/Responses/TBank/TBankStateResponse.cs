using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.TBank;

public class TBankStateResponse
{
    [JsonPropertyName("platformReferenceNumber")]
    public string? PlatformReferenceNumber { get; set; }

    [JsonPropertyName("transferState")]
    public TBankTransferState? TransferState { get; set; }

    [JsonPropertyName("checkDate")]
    public string? CheckDate { get; set; }
}
