using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Agents;

public sealed class AgentServiceAccess
{
    public string AgentId { get; private set; } = default!;
    public string ServiceId { get; private set; } = default!;
    public bool Enabled { get; private set; } = true;
    public int FeePermille { get; private set; }
    public long FeeFlatMinor { get; private set; }

    private AgentServiceAccess() { }

    public AgentServiceAccess(string agentId, string serviceId, bool enabled = true, int feePermille = 0, long feeFlatMinor = 0)
    {
        AgentId = agentId;
        ServiceId = serviceId;
        Enabled = enabled;
        FeePermille = feePermille;
        FeeFlatMinor = feeFlatMinor;
    }

    public static AgentServiceAccess Create(string agentId, string serviceId, bool enabled = true, int feePermille = 0, long feeFlatMinor = 0)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new DomainException("AgentId обязателен.");
        if (string.IsNullOrWhiteSpace(serviceId)) throw new DomainException("ServiceId обязателен.");
        return new AgentServiceAccess(agentId, serviceId, enabled, feePermille, feeFlatMinor);
    }

    public void UpdateProfile(bool? enabled = null, int? feePermille = null, long? feeFlatMinor = null)
    {
        if (enabled.HasValue) Enabled = enabled.Value;
        if (feePermille.HasValue) FeePermille = feePermille.Value;
        if (feeFlatMinor.HasValue) FeeFlatMinor = feeFlatMinor.Value;
    }

    public long CalculateFee(long amountMinor) =>
        amountMinor * FeePermille / 10000 + FeeFlatMinor;
}
