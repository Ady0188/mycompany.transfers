using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.TBank;

public class TBankConversionRateBuy
{
    [JsonPropertyName("originatorCurrency")]
    public string OriginatorCurrency { get; set; } = string.Empty;

    [JsonPropertyName("settlementCurrency")]
    public string SettlementCurrency { get; set; } = string.Empty;

    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    [JsonPropertyName("baseRate")]
    public decimal BaseRate { get; set; }
}
