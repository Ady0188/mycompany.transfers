namespace MyCompany.Transfers.Infrastructure.Providers.Responses.FIMI;

internal class FimiPosDepositResponse
{
    public string authorizationNumber { get; set; }
    public string approvalCode { get; set; }
    public string? rrn { get; set; }
    public string? orig_result { get; set; }
    public int result { get; set; }
    public string description { get; set; }
    public long requestId { get; set; }
}
