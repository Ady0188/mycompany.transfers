using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Api.Models;

public sealed class SendCredentialsResponse
{
    [JsonPropertyName("archivePassword")]
    public string ArchivePassword { get; set; } = "";
}
