using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Agents.Queries;

public sealed record GetAgentByIdQuery(string AgentId) : IRequest<ErrorOr<Agent>>;

public sealed class GetAgentByIdQueryHandler : IRequestHandler<GetAgentByIdQuery, ErrorOr<Agent>>
{
    private readonly IAgentReadRepository _read;

    public GetAgentByIdQueryHandler(IAgentReadRepository read) => _read = read;

    public async Task<ErrorOr<Agent>> Handle(GetAgentByIdQuery request, CancellationToken ct)
    {
        var agent = await _read.GetByIdAsync(request.AgentId, ct);
        if (agent is null)
            return AppErrors.Agents.NotFound(request.AgentId);
        return agent;
    }
}
