using System.Xml.Serialization;

namespace MyCompany.Transfers.Contract.Tillabuy.Responses;

[XmlRoot(ElementName = "Response")]
public class NKOCheckResponse
{
    [XmlElement(ElementName = "Result")]
    public string Result { get; set; }

    [XmlElement(ElementName = "ErrCode")]
    public int ErrCode { get; set; }

    [XmlElement(ElementName = "PaymExtId")]
    public string PaymExtId { get; set; }

    [XmlElement(ElementName = "RequestId")]
    public string RequestId { get; set; }

    [XmlElement(ElementName = "TechInfo")]
    public string TechInfo { get; set; }

    [XmlElement(ElementName = "Description")]
    public string Description { get; set; }

    [XmlElement(ElementName = "Balance")]
    public decimal Balance { get; set; }

    [XmlElement(ElementName = "ExtInfo")]
    public ExtInfo ExtInfo { get; set; }


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

public class CheckTag
{
    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; }

    [XmlText]
    public string Value { get; set; }
}

public class ExtInfo
{
    [XmlElement(ElementName = "Tag")]
    public CheckTag[] Tags { get; set; }
}