namespace MyCompany.Transfers.Domain.Agents;

public sealed class AgentCurrencyAccess
{
    public string AgentId { get; private set; } = default!;
    public string Currency { get; private set; } = default!;
    public bool Enabled { get; private set; } = true;

    private AgentCurrencyAccess() { }

    public AgentCurrencyAccess(string agentId, string currency, bool enabled = true)
    {
        AgentId = agentId;
        Currency = currency;
        Enabled = enabled;
    }
}
