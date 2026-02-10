using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

public class ExchangeResponseObject
{
    [JsonPropertyName("exchangeId")]
    public string ExchangeId { get; set; }

    [JsonPropertyName("amountForeign")]
    public decimal AmountForeign { get; set; }

    [JsonPropertyName("currencyForeign")]
    public string CurrencyForeign { get; set; }

    [JsonPropertyName("amountTRY")]
    public decimal AmountTRY { get; set; }

    [JsonPropertyName("exchangeRate")]
    public decimal ExchangeRate { get; set; }

    [JsonPropertyName("commercial")]
    public bool Commercial { get; set; }
}

