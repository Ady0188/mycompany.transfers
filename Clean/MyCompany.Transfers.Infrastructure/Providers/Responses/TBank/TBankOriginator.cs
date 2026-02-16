using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.TBank;

public class TBankOriginator
{
    [JsonPropertyName("identification")]
    public TBankIdentification? Identification { get; set; }

    [JsonPropertyName("participant")]
    public TBankParticipant? Participant { get; set; }
}
