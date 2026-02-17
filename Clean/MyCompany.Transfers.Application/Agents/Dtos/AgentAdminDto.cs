using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Agents.Dtos;

public sealed record AgentAdminDto(
    string Id,
    string TimeZoneId,
    string SettingsJson)
{
    public static AgentAdminDto FromDomain(Agent a) =>
        new(a.Id, a.TimeZoneId, a.SettingsJson);
}

