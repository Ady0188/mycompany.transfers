using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.TBank;

public class TBankOriginator
{
    [JsonPropertyName("identification")]
    public TBankIdentification Identification { get; set; }

    [JsonPropertyName("participant")]
    public TBankParticipant Participant { get; set; }

    [JsonPropertyName("additionalIdentification")]
    public List<TBankAdditionalIdentification> AdditionalIdentification { get; set; } = [];

    [JsonIgnore]
    public string Name { get; set; }

    [JsonIgnore]
    public string CustomNameFieldKey { get; set; }
}
