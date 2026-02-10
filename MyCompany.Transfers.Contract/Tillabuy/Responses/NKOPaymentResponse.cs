using System.Xml.Serialization;

namespace MyCompany.Transfers.Contract.Tillabuy.Responses;

[XmlRoot(ElementName = "Response")]
public class NKOPaymentResponse
{
    [XmlElement(ElementName = "Result")]
    public string Result { get; set; }

    [XmlElement(ElementName = "ErrCode")]
    public int ErrCode { get; set; }

    [XmlElement(ElementName = "PaymExtId")]
    public string PaymExtId { get; set; }

    [XmlElement(ElementName = "RequestId")]
    public string RequestId { get; set; }

    [XmlElement(ElementName = "Description")]
    public string Description { get; set; }

    [XmlElement(ElementName = "TechInfo")]
    public string TechInfo { get; set; }

    [XmlElement(ElementName = "PaymNumb")]
    public string PaymNumb { get; set; }

    [XmlElement(ElementName = "BillRegId")]
    public string BillRegId { get; set; }

    [XmlElement(ElementName = "PaymDate")]
    public string PaymDate { get; set; }

    [XmlElement(ElementName = "Balance")]
    public decimal Balance { get; set; }



    [XmlElement(ElementName = "ExchangeRate")]
    public decimal? ExchangeRate { get; set; }

    [XmlIgnore]
    public bool ExchangeRateSpecified => ExchangeRate.HasValue;

    [XmlElement(ElementName = "Currency")]
    public string Currency { get; set; }

    [XmlIgnore]
    public bool CurrencySpecified => !string.IsNullOrWhiteSpace(Currency);

    [XmlElement(ElementName = "CreditAmount")]
    public long? CreditAmount { get; set; }

    [XmlIgnore]
    public bool CreditAmountSpecified => CreditAmount.HasValue;




    public string StringResult { get; set; }
}