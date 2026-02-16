namespace MyCompany.Transfers.Contract.Tillabuy.Requests;

public class TillabuyTrn
{
    public string Service { get; set; } = string.Empty;
    public string Agent { get; set; } = string.Empty;
    public string PaymentExtId { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
    public string TerminalId { get; set; } = string.Empty;
    public string TerminalType { get; set; } = string.Empty;
    public long Amount { get; set; }
    public decimal FeeSum { get; set; }
    public string? TermExtTime { get; set; }
    public DateTime TermTime { get; set; } = DateTime.Now;
    public string? RequestId { get; set; }
    public string Currency { get; set; } = "RUB";
    public string PayoutCurrency { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
}
