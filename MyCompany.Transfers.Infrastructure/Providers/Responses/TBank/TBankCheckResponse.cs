using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Infrastructure.Providers.Responses.TBank;

public class TBankCheckResponse
{
    [JsonPropertyName("platformReferenceNumber")]
    public string? PlatformReferenceNumber { get; set; }

    [JsonPropertyName("originator")]
    public TBankOriginator? Originator { get; set; }

    [JsonPropertyName("receiver")]
    public TBankReceiver? Receiver { get; set; }

    [JsonPropertyName("paymentAmount")]
    public TBankAllAmount? PaymentAmount { get; set; }

    [JsonPropertyName("feeAmount")]
    public List<TBankFeeAmount>? FeeAmount { get; set; }

    [JsonPropertyName("settlementAmount")]
    public TBankAllAmount? SettlementAmount { get; set; }

    [JsonPropertyName("receivingAmount")]
    public TBankAllAmount? ReceivingAmount { get; set; }

    [JsonPropertyName("checkDate")]
    public string? CheckDate { get; set; }

    [JsonPropertyName("transferState")]
    public TBankTransferState? TransferState { get; set; }

    [JsonPropertyName("conversionRateBuy")]
    public TBankConversionRateBuy? ConversionRateBuy { get; set; }
}