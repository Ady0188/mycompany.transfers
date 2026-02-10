using ErrorOr;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;
using MediatR;

namespace MyCompany.Transfers.Application.Agents.Queries;

public sealed record GetAgentByIdQuery(string AgentId): IRequest<ErrorOr<Agent>>;
public class GetAgentByIdQueryHandler : IRequestHandler<GetAgentByIdQuery, ErrorOr<Agent>>
{
    private readonly IAgentReadRepository _read;

    public GetAgentByIdQueryHandler(IAgentReadRepository read)
    {
        _read = read;
    }

    public async Task<ErrorOr<Agent>> Handle(GetAgentByIdQuery request, CancellationToken cancellationToken)
    {
        var agent = await _read.GetByIdAsync(request.AgentId, cancellationToken);

        if (agent is null)
            return Error.NotFound();

        return agent;
    }
}