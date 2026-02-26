using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Agents;

public sealed class AgentDailyBalance : IEntity
{
    public Guid Id { get; private set; }
    public string AgentId { get; private set; } = default!;
    public DateTime Date { get; private set; }
    public string Currency { get; private set; } = default!;
    public long OpeningBalanceMinor { get; private set; }
    public long ClosingBalanceMinor { get; private set; }
    /// <summary>Часовой пояс, в котором считается этот дневной баланс.</summary>
    public string TimeZoneId { get; private set; } = default!;
    /// <summary>Тип календаря: локальный банк или календарь агента.</summary>
    public DailyBalanceScope Scope { get; private set; }

    private AgentDailyBalance() { }

    private AgentDailyBalance(
        Guid id,
        string agentId,
        DateTime date,
        string currency,
        long openingBalanceMinor,
        long closingBalanceMinor,
        string timeZoneId,
        DailyBalanceScope scope)
    {
        Id = id;
        AgentId = agentId;
        Date = date.Date;
        Currency = currency;
        OpeningBalanceMinor = openingBalanceMinor;
        ClosingBalanceMinor = closingBalanceMinor;
        TimeZoneId = timeZoneId;
        Scope = scope;
    }

    public static AgentDailyBalance Create(
        string agentId,
        DateTime date,
        string currency,
        long openingBalanceMinor,
        long closingBalanceMinor,
        string timeZoneId,
        DailyBalanceScope scope)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            throw new DomainException("AgentId is required for daily balance.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required for daily balance.");
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new DomainException("TimeZoneId is required for daily balance.");

        return new AgentDailyBalance(
            Guid.NewGuid(),
            agentId,
            date.Date,
            currency,
            openingBalanceMinor,
            closingBalanceMinor,
            timeZoneId,
            scope);
    }

    public void UpdateClosingBalance(long closingBalanceMinor)
    {
        ClosingBalanceMinor = closingBalanceMinor;
    }
}

