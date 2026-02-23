using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

/// <summary>Модель определения счёта для админ API.</summary>
public sealed class AccountDefinitionAdminDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("regex")]
    public string? Regex { get; set; }

    [JsonPropertyName("normalize")]
    public int Normalize { get; set; } // 0=None, 1=Trim, 2=DigitsOnly

    [JsonPropertyName("algorithm")]
    public int Algorithm { get; set; } // 0=None, 1=Luhn

    [JsonPropertyName("minLength")]
    public int? MinLength { get; set; }

    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }
}
