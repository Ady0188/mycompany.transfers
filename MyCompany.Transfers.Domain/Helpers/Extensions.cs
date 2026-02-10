using MyCompany.Transfers.Domain.Transfers;
using System.Globalization;

namespace MyCompany.Transfers.Domain.Helpers;

public static class Extensions
{
    private static readonly Dictionary<TransferStatus, string> Statuses = new()
    {
        { TransferStatus.NEW, "NEW" },
        { TransferStatus.PREPARED, "PREPARED" },
        { TransferStatus.CONFIRMED, "CONFIRMED" },
        { TransferStatus.SUCCESS, "SUCCESS" },
        { TransferStatus.FAILED, "FAILED" },
        { TransferStatus.EXPIRED, "EXPIRED" },
        { TransferStatus.FRAUD, "FRAUD" }
    };

    public static string ToResponse(this TransferStatus status)
    {
        return Statuses.TryGetValue(status, out var result)
            ? result
            : status.ToString();
    }
}
