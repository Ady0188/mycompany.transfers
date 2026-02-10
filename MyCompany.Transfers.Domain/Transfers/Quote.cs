using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Transfers;

public sealed class Quote
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public Money Total { get; private set; }
    public Money Fee { get; private set; }
    public Money ProviderFee { get; private set; }

    public Money CreditedAmount { get; private set; }

    public decimal? ExchangeRate { get; private set; } 
    public DateTimeOffset? RateTimestampUtc { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    private Quote() { }
    //public static Quote Create(string currency, long totalMinor, long feeMinor, TimeSpan ttl, DateTimeOffset now)
    //    => new() { Total = new Money(totalMinor, currency), Fee = new Money(feeMinor, currency), ExpiresAt = now.Add(ttl) };

    public static Quote Create(
        Money total,
        Money fee,
        Money providerFee,
        Money creditedAmount,
        decimal? exchangeRate,
        DateTimeOffset? rateTimestampUtc,
        TimeSpan ttl,
        DateTimeOffset now)
    {
        if (total.Currency != fee.Currency)
            throw new DomainException("Total and Fee currency mismatch");

        if (creditedAmount.Minor < 0)
            throw new DomainException("CreditedAmount cannot be negative");

        if (exchangeRate is not null && exchangeRate <= 0)
            throw new DomainException("ExchangeRate must be positive");

        return new Quote
        {
            Total = total,
            Fee = fee,
            ProviderFee = providerFee,
            CreditedAmount = creditedAmount,
            ExchangeRate = exchangeRate,
            RateTimestampUtc = rateTimestampUtc,
            ExpiresAt = now.Add(ttl)
        };
    }

    public bool IsExpired(DateTimeOffset now) => now > ExpiresAt;
}
