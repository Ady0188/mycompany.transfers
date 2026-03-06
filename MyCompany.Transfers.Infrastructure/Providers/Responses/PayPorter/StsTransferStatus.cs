namespace MyCompany.Transfers.Infrastructure.Providers.Responses.PayPorter;

public class StsTransferStatus
{
    public int StatusCode { get; set; }
    public string StatusName { get; set; }
    public string StatusDescription { get; set; }
    public string StatusReasonMessageCode { get; set; }
    public string StatusReasonMessageDetail { get; set; }
}