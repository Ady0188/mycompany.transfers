using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

/// <summary>Модель БИН для админ API. С клиента отправляются только Prefix, Code, Name.</summary>
public sealed class BinAdminDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = "";

    [JsonPropertyName("len")]
    public int Len { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}
