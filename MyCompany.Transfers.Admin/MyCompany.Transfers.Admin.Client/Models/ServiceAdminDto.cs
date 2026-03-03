using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

/// <summary>Модель услуги для админ API (совпадает с ответом API).</summary>
public sealed class ServiceAdminDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("providerId")]
    public string ProviderId { get; set; } = "";

    [JsonPropertyName("providerServiceId")]
    public string ProviderServiceId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("allowedCurrencies")]
    public string[] AllowedCurrencies { get; set; } = Array.Empty<string>();

    [JsonPropertyName("fxRounding")]
    public string? FxRounding { get; set; }

    [JsonPropertyName("minAmountMinor")]
    public long MinAmountMinor { get; set; }

    [JsonPropertyName("maxAmountMinor")]
    public long MaxAmountMinor { get; set; }

    [JsonPropertyName("accountDefinitionId")]
    public Guid AccountDefinitionId { get; set; }

    [JsonPropertyName("parameters")]
    public List<ServiceParamDefinitionDto> Parameters { get; set; } = new();
}

public sealed class ServiceParamDefinitionDto
{
    [JsonPropertyName("parameterId")]
    public string ParameterId { get; set; } = "";

    [JsonPropertyName("required")]
    public bool Required { get; set; }
}
