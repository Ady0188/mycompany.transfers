using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Domain.Providers;

public sealed class ProviderErrorCode
{
    public long Id { get; set; }
    public string ProviderId { get; set; } = default!;
    public string ProviderCode { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ProviderResultKind Kind { get; set; }
    public TransferStatus Status { get; set; }
    public int? ErrorCode { get; set; }
}
