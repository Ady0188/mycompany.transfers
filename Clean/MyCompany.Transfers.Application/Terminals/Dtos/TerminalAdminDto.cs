using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Terminals.Dtos;

public sealed record TerminalAdminDto(
    string Id,
    string AgentId,
    string Name,
    string ApiKey,
    string Secret,
    bool Active)
{
    public static TerminalAdminDto FromDomain(Terminal t) =>
        new(t.Id, t.AgentId, t.Name, t.ApiKey, t.Secret, t.Active);
}
