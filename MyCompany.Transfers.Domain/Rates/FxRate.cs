namespace MyCompany.Transfers.Domain.Rates;

public sealed class FxRate
{
    public long Id { get; private set; }
    public string BaseCurrency { get; private set; } = default!;   // из какой валюты
    public string QuoteCurrency { get; private set; } = default!;  // в какую валюту
    public decimal Rate { get; private set; }                      // 1 base = Rate quote
    public DateTimeOffset UpdatedAtUtc { get; private set; }       // когда получили курс
    public string Source { get; private set; } = "manual";         // источник (manual/ecb/...
    public bool IsActive { get; private set; } = true;

    private FxRate() { }
    public FxRate(string baseCcy, string quoteCcy, decimal rate, DateTimeOffset updatedAtUtc, string source = "manual", bool isActive = true)
        => (BaseCurrency, QuoteCurrency, Rate, UpdatedAtUtc, Source, IsActive) = (baseCcy, quoteCcy, rate, updatedAtUtc, source, isActive);
}