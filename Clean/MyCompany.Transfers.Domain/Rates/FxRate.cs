namespace MyCompany.Transfers.Domain.Rates;

public sealed class FxRate
{
    public long Id { get; private set; }
    public string AgentId { get; private set; } = default!;
    public string BaseCurrency { get; private set; } = default!;
    public string QuoteCurrency { get; private set; } = default!;
    public decimal Rate { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string Source { get; private set; } = "manual";
    public bool IsActive { get; private set; } = true;

    private FxRate() { }

    public FxRate(string agentId, string baseCcy, string quoteCcy, decimal rate, DateTimeOffset updatedAtUtc, string source = "manual", bool isActive = true)
    {
        AgentId = agentId;
        BaseCurrency = baseCcy;
        QuoteCurrency = quoteCcy;
        Rate = rate;
        UpdatedAtUtc = updatedAtUtc;
        Source = source;
        IsActive = isActive;
    }
}
