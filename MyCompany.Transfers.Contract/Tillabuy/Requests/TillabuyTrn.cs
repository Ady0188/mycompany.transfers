namespace MyCompany.Transfers.Contract.Tillabuy.Requests;

public class TillabuyTrn
{
    public int Id { get; set; }
    public string Service { get; set; }
    public string Agent { get; set; }
    public string PaymentExtId { get; set; }
    public string Account { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
    public string TerminalId { get; set; }
    public string TerminalType { get; set; }
    public long Amount { get; set; }
    public int DtAmount { get; set; }
    public decimal FeeSum { get; set; }
    public string? TermExtTime { get; set; }
    public DateTime TermTime { get; set; } = DateTime.Now;
    public DateTime NkoTime { get; set; }
    public string PaymNumb { get; set; } = string.Empty;
    public string? RequestId { get; set; }
    public string? NkoResult { get; set; }
    public string? ProviderMessage { get; set; }
    public int NkoErrorCode { get; set; } = -1;
    public int Status { get; set; } = 1;
    public decimal Commission { get; set; }
    public string UnicumCode { get; set; } = string.Empty;
    public string Suip { get; set; } = string.Empty;
    public int BalanceSum { get; set; }
    public decimal Rate { get; set; }
    public string Currency { get; set; }
    public string PayoutCurrency { get; set; }
    public int CreditAmount { get; set; }
    public string Pan { get; set; }
    public string SenderName { get; set; }
    public string? RecipientName { get; set; }
}