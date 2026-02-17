using MediatR;
using MyCompany.Transfers.Application.Agents.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Agents.Queries;

public sealed record GetAgentsQuery() : IRequest<IReadOnlyList<AgentAdminDto>>;

public sealed class GetAgentsQueryHandler : IRequestHandler<GetAgentsQuery, IReadOnlyList<AgentAdminDto>>
{
    private readonly IAgentRepository _agents;

    public GetAgentsQueryHandler(IAgentRepository agents) => _agents = agents;

    public async Task<IReadOnlyList<AgentAdminDto>> Handle(GetAgentsQuery request, CancellationToken ct)
    {
        var list = await _agents.GetAllAsync(ct);
        return list.Select(AgentAdminDto.FromDomain).ToList();
    }
}

