using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.TBank
{
    public class TBankConversionRateSell
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("settlementCurrency")]
        public string SettlementCurrency { get; set; } = string.Empty;

        [JsonPropertyName("receivingCurrency")]
        public string ReceivingCurrency { get; set; } = string.Empty;

        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }

        [JsonPropertyName("baseRate")]
        public decimal BaseRate { get; set; }

        public bool ShouldSerializeBaseRate()
            => BaseRate > 0;
    }
}