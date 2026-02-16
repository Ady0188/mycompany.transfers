namespace MyCompany.Transfers.Contract.Core.Requests;

public class PrepareRequest
{
    public string ExternalId { get; set; } = string.Empty;
    public required TransferMethod Method { get; set; }
    public string Account { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? PayoutCurrency { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
}
