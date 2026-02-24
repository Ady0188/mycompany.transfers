using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Agents.Dtos;

public sealed record AgentAdminDto(
    string Id,
    string Name,
    string Account,
    string TimeZoneId,
    string SettingsJson,
    string? PartnerEmail)
{
    public static AgentAdminDto FromDomain(Agent a) =>
        new(a.Id, a.Name, a.Account, a.TimeZoneId, a.SettingsJson, a.PartnerEmail);
}

