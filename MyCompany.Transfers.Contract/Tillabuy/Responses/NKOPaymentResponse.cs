using System.Xml.Serialization;

namespace MyCompany.Transfers.Contract.Tillabuy.Responses;

[XmlRoot(ElementName = "Response")]
public class NKOPaymentResponse
{
    [XmlElement(ElementName = "Result")]
    public string Result { get; set; } = string.Empty;

    [XmlElement(ElementName = "ErrCode")]
    public int ErrCode { get; set; }

    [XmlElement(ElementName = "PaymExtId")]
    public string PaymExtId { get; set; } = string.Empty;

    [XmlElement(ElementName = "RequestId")]
    public string RequestId { get; set; } = string.Empty;

    [XmlElement(ElementName = "Description")]
    public string Description { get; set; } = string.Empty;

    [XmlElement(ElementName = "TechInfo")]
    public string TechInfo { get; set; } = string.Empty;

    [XmlElement(ElementName = "PaymNumb")]
    public string PaymNumb { get; set; } = string.Empty;

    [XmlElement(ElementName = "BillRegId")]
    public string BillRegId { get; set; } = string.Empty;

    [XmlElement(ElementName = "PaymDate")]
    public string PaymDate { get; set; } = string.Empty;

    [XmlElement(ElementName = "Balance")]
    public decimal Balance { get; set; }

    [XmlElement(ElementName = "ExchangeRate")]
    public decimal? ExchangeRate { get; set; }

    [XmlIgnore]
    public bool ExchangeRateSpecified => ExchangeRate.HasValue;

    [XmlElement(ElementName = "Currency")]
    public string Currency { get; set; } = string.Empty;

    [XmlIgnore]
    public bool CurrencySpecified => !string.IsNullOrWhiteSpace(Currency);

    [XmlElement(ElementName = "CreditAmount")]
    public long? CreditAmount { get; set; }

    [XmlIgnore]
    public bool CreditAmountSpecified => CreditAmount.HasValue;
}
