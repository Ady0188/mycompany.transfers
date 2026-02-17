using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Access.ServiceAccess.Dtos;

public sealed record AgentServiceAccessAdminDto(
    string AgentId,
    string ServiceId,
    bool Enabled,
    int FeePermille,
    long FeeFlatMinor)
{
    public static AgentServiceAccessAdminDto FromDomain(AgentServiceAccess e) =>
        new(e.AgentId, e.ServiceId, e.Enabled, e.FeePermille, e.FeeFlatMinor);
}
