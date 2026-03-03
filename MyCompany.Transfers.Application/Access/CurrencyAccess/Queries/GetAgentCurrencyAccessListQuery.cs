using MediatR;
using MyCompany.Transfers.Application.Access.CurrencyAccess.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Access.CurrencyAccess.Queries;

public sealed record GetAgentCurrencyAccessListQuery() : IRequest<IReadOnlyList<AgentCurrencyAccessAdminDto>>;

public sealed class GetAgentCurrencyAccessListQueryHandler : IRequestHandler<GetAgentCurrencyAccessListQuery, IReadOnlyList<AgentCurrencyAccessAdminDto>>
{
    private readonly IAccessRepository _access;

    public GetAgentCurrencyAccessListQueryHandler(IAccessRepository access) => _access = access;

    public async Task<IReadOnlyList<AgentCurrencyAccessAdminDto>> Handle(GetAgentCurrencyAccessListQuery request, CancellationToken ct)
    {
        var list = await _access.GetAllAgentCurrencyAccessAsync(ct);
        return list.Select(AgentCurrencyAccessAdminDto.FromDomain).ToList();
    }
}
