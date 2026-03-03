using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Terminals.Dtos;

public sealed record TerminalAdminDto(
    string Id,
    string AgentId,
    string Name,
    string ApiKey,
    string Secret,
    bool Active,
    string? AgentPartnerEmail)
{
    /// <summary>Маппинг из домена. При <paramref name="maskSecret"/> true Secret не возвращается (для API/клиента).</summary>
    public static TerminalAdminDto FromDomain(Terminal t, string? agentPartnerEmail = null, bool maskSecret = false) =>
        new(t.Id, t.AgentId, t.Name, t.ApiKey, maskSecret ? "" : t.Secret, t.Active, agentPartnerEmail);
}
