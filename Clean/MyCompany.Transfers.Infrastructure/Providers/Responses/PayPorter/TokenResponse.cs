using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

public class TokenResponse
{
    [JsonPropertyName("header")]
    public Header Header { get; set; } = null!;

    [JsonPropertyName("responseObject")]
    public TokenResponseObject ResponseObject { get; set; } = null!;
}
