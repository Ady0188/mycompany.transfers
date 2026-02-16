using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Domain.Helpers;

public static class TransferStatusExtensions
{
    private static readonly Dictionary<TransferStatus, string> Statuses = new()
    {
        { TransferStatus.NEW, "NEW" },
        { TransferStatus.PREPARED, "PREPARED" },
        { TransferStatus.CONFIRMED, "CONFIRMED" },
        { TransferStatus.SUCCESS, "SUCCESS" },
        { TransferStatus.TECHNICAL, "TECHNICAL" },
        { TransferStatus.FAILED, "FAILED" },
        { TransferStatus.EXPIRED, "EXPIRED" },
        { TransferStatus.FRAUD, "FRAUD" }
    };

    public static string ToResponse(this TransferStatus status) =>
        Statuses.TryGetValue(status, out var result) ? result : status.ToString();
}
