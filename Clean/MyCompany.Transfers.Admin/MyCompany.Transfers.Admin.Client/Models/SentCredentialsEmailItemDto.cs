using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

public sealed class SentCredentialsEmailItemDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("terminalId")]
    public string TerminalId { get; set; } = "";

    [JsonPropertyName("toEmail")]
    public string ToEmail { get; set; } = "";

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = "";

    [JsonPropertyName("sentAtUtc")]
    public DateTime SentAtUtc { get; set; }
}
