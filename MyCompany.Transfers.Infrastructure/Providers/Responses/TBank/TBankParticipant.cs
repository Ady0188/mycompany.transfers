using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.TBank;

public class TBankParticipant
{
    [JsonPropertyName("participantId")]
    public long ParticipantId { get; set; }
}
