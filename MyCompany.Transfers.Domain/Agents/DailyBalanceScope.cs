namespace MyCompany.Transfers.Domain.Agents;

public enum DailyBalanceScope
{
    /// <summary>Локальный (внутренний) календарь банка.</summary>
    Local = 0,

    /// <summary>Календарь в часовом поясе агента.</summary>
    Agent = 1
}

