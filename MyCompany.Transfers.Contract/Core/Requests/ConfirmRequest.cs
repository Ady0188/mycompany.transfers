namespace MyCompany.Transfers.Contract.Core.Requests;

public sealed class ConfirmRequest
{
    public string ExternalId { get; set; } = string.Empty;
    public string QuotationId { get; set; } = string.Empty;
}
