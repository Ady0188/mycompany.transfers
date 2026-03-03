using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

public sealed class SendCredentialsResponse
{
    [JsonPropertyName("archivePassword")]
    public string? ArchivePassword { get; set; }
}
