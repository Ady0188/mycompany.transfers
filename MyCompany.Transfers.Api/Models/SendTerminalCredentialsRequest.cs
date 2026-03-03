using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Api.Models;

public sealed class SendTerminalCredentialsRequest
{
    [JsonPropertyName("toEmail")]
    public string ToEmail { get; set; } = "";

    [JsonPropertyName("body")]
    public string Body { get; set; } = "";

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }
}
