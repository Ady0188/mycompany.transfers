namespace MyCompany.Transfers.Domain.Agents;

public sealed class AgentServiceAccess
{
    public string AgentId { get; private set; } = default!;
    public string ServiceId { get; private set; } = default!;
    public bool Enabled { get; private set; } = true;

    public int FeePermille { get; private set; }
    public long FeeFlatMinor { get; private set; }

    private AgentServiceAccess() { }

    public AgentServiceAccess(string agentId, string serviceId,
        bool enabled = true, int feePermille = 0, long feeFlatMinor = 0)
    {
        AgentId = agentId;
        ServiceId = serviceId;
        Enabled = enabled;
        FeePermille = feePermille;
        FeeFlatMinor = feeFlatMinor;
    }

    public long CalculateFee(long amountMinor) =>
        amountMinor * FeePermille / 10000 + FeeFlatMinor;
}

//public sealed class AgentServiceAccess
//{
//    public string AgentId { get; private set; } = default!;
//    public string ServiceId { get; private set; } = default!;
//    public bool Enabled { get; private set; } = true;

//    private AgentServiceAccess() { }
//    public AgentServiceAccess(string agentId, string serviceId, bool enabled = true)
//    { AgentId = agentId; ServiceId = serviceId; Enabled = enabled; }
//}