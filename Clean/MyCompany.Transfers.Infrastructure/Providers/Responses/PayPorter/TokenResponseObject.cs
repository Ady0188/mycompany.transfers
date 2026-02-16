using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

public class TokenResponseObject
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
}
