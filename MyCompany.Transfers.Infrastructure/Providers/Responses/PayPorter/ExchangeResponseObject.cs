using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

public class ExchangeResponseObject
{
    [JsonPropertyName("exchangeId")]
    public string ExchangeId { get; set; } = string.Empty;

    [JsonPropertyName("exchangeRate")]
    public decimal ExchangeRate { get; set; }
}
