using MyCompany.Transfers.Domain.Common;

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

    public static AgentCurrencyAccess Create(string agentId, string currency, bool enabled = true)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new DomainException("AgentId обязателен.");
        if (string.IsNullOrWhiteSpace(currency)) throw new DomainException("Currency обязателен.");
        return new AgentCurrencyAccess(agentId, currency, enabled);
    }

    public void UpdateProfile(bool? enabled = null)
    {
        if (enabled.HasValue) Enabled = enabled.Value;
    }
}
