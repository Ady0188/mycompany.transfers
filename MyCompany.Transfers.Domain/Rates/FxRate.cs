using MyCompany.Transfers.Domain.Common;

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

    public static FxRate Create(string agentId, string baseCcy, string quoteCcy, decimal rate, DateTimeOffset updatedAtUtc, string source = "manual", bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new DomainException("AgentId обязателен.");
        if (string.IsNullOrWhiteSpace(baseCcy)) throw new DomainException("BaseCurrency обязателен.");
        if (string.IsNullOrWhiteSpace(quoteCcy)) throw new DomainException("QuoteCurrency обязателен.");
        if (rate <= 0) throw new DomainException("Курс должен быть положительным.");
        return new FxRate(agentId, baseCcy, quoteCcy, rate, updatedAtUtc, source, isActive);
    }

    public void UpdateRate(decimal rate, DateTimeOffset updatedAtUtc, string? source = null)
    {
        if (rate <= 0) throw new DomainException("Курс должен быть положительным.");
        Rate = rate;
        UpdatedAtUtc = updatedAtUtc;
        if (source != null) Source = source;
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
