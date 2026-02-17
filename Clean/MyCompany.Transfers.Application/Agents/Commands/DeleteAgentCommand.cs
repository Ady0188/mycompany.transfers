using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Agents.Commands;

public sealed record DeleteAgentCommand(string Id) : IRequest<ErrorOr<Unit>>;

public sealed class DeleteAgentCommandHandler : IRequestHandler<DeleteAgentCommand, ErrorOr<Unit>>
{
    private readonly IAgentRepository _agents;
    private readonly IUnitOfWork _uow;

    public DeleteAgentCommandHandler(
        IAgentRepository agents,
        IUnitOfWork uow)
    {
        _agents = agents;
        _uow = uow;
    }

    public async Task<ErrorOr<Unit>> Handle(DeleteAgentCommand m, CancellationToken ct)
    {
        var agent = await _agents.GetForUpdateAsync(m.Id, ct);
        if (agent is null)
            return AppErrors.Agents.NotFound(m.Id);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _agents.Remove(agent);
            return Task.FromResult(true);
        }, ct);

        return Unit.Value;
    }
}

