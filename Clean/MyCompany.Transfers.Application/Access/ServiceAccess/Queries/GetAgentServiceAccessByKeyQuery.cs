using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Access.ServiceAccess.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Access.ServiceAccess.Queries;

public sealed record GetAgentServiceAccessByKeyQuery(string AgentId, string ServiceId) : IRequest<ErrorOr<AgentServiceAccessAdminDto>>;

public sealed class GetAgentServiceAccessByKeyQueryHandler : IRequestHandler<GetAgentServiceAccessByKeyQuery, ErrorOr<AgentServiceAccessAdminDto>>
{
    private readonly IAccessRepository _access;

    public GetAgentServiceAccessByKeyQueryHandler(IAccessRepository access) => _access = access;

    public async Task<ErrorOr<AgentServiceAccessAdminDto>> Handle(GetAgentServiceAccessByKeyQuery request, CancellationToken ct)
    {
        var entity = await _access.GetAgentServiceAccessForUpdateAsync(request.AgentId, request.ServiceId, ct);
        if (entity == null)
            return Error.NotFound(description: "Запись доступа агент–услуга не найдена.");
        return AgentServiceAccessAdminDto.FromDomain(entity);
    }
}
