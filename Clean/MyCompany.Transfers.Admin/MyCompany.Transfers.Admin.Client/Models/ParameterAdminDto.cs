using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

/// <summary>Модель параметра (ParamDefinition) для админ API.</summary>
public sealed class ParameterAdminDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("regex")]
    public string? Regex { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; } = true;
}
