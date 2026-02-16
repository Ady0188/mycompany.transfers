using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Transfers;

public sealed record Money(long Minor, string Currency)
{
    public static Money operator +(Money a, Money b) =>
        a.Currency == b.Currency
            ? new Money(a.Minor + b.Minor, a.Currency)
            : throw new DomainException("Currency mismatch");
}
