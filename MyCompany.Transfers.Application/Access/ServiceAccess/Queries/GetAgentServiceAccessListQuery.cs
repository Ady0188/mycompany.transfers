using MediatR;
using MyCompany.Transfers.Application.Access.ServiceAccess.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Access.ServiceAccess.Queries;

public sealed record GetAgentServiceAccessListQuery() : IRequest<IReadOnlyList<AgentServiceAccessAdminDto>>;

public sealed class GetAgentServiceAccessListQueryHandler : IRequestHandler<GetAgentServiceAccessListQuery, IReadOnlyList<AgentServiceAccessAdminDto>>
{
    private readonly IAccessRepository _access;

    public GetAgentServiceAccessListQueryHandler(IAccessRepository access) => _access = access;

    public async Task<IReadOnlyList<AgentServiceAccessAdminDto>> Handle(GetAgentServiceAccessListQuery request, CancellationToken ct)
    {
        var list = await _access.GetAllAgentServiceAccessAsync(ct);
        return list.Select(AgentServiceAccessAdminDto.FromDomain).ToList();
    }
}
