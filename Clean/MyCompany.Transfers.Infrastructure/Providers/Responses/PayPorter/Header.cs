using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

public class Header
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
