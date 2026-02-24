using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Terminals.Dtos;

/// <summary>DTO для списка терминалов (без ApiKey и Secret).</summary>
public sealed record TerminalListDto(
    string Id,
    string AgentId,
    string Name,
    bool Active)
{
    public static TerminalListDto FromDomain(Terminal t) =>
        new(t.Id, t.AgentId, t.Name, t.Active);
}
