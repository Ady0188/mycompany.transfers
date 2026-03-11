namespace MyCompany.Transfers.Contract.Solidarnost;

/// <summary>
/// Настройки протокола Solidarnost (агент, услуга, терминал для маппинга на общий API).
/// </summary>
public class SolidarnostOptions
{
    public const string SectionName = "Solidarnost";

    public string AgentId { get; set; } = string.Empty;
    public string PanServiceId { get; set; } = string.Empty;
    public string PhoneServiceId { get; set; } = string.Empty;
    public string TerminalId { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = "RUB";
}
