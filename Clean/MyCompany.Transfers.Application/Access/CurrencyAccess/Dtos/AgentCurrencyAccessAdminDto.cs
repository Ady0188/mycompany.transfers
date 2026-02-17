using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Access.CurrencyAccess.Dtos;

public sealed record AgentCurrencyAccessAdminDto(
    string AgentId,
    string Currency,
    bool Enabled)
{
    public static AgentCurrencyAccessAdminDto FromDomain(AgentCurrencyAccess e) =>
        new(e.AgentId, e.Currency, e.Enabled);
}
