using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.TBank;

public class TBankReceiver
{
    [JsonPropertyName("identification")]
    public TBankIdentification? Identification { get; set; }

    [JsonPropertyName("currencies")]
    public List<string>? Currencies { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("additionalIdentification")]
    public List<TBankAdditionalIdentification>? AdditionalIdentification { get; set; }

    [JsonPropertyName("participant")]
    public TBankParticipant? Participant { get; set; }

    [JsonIgnore]
    public string Name { get; set; }

    [JsonIgnore]
    public string CustomNameFieldKey { get; set; }
}