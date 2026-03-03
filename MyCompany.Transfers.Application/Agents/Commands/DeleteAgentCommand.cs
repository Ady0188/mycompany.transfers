using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Agents.Commands;

public sealed record DeleteAgentCommand(string Id) : IRequest<ErrorOr<Unit>>;

public sealed class DeleteAgentCommandHandler : IRequestHandler<DeleteAgentCommand, ErrorOr<Unit>>
{
    private readonly IAgentRepository _agents;
    private readonly ITerminalRepository _terminals;
    private readonly IAccessRepository _access;
    private readonly IUnitOfWork _uow;

    public DeleteAgentCommandHandler(
        IAgentRepository agents,
        ITerminalRepository terminals,
        IAccessRepository access,
        IUnitOfWork uow)
    {
        _agents = agents;
        _terminals = terminals;
        _access = access;
        _uow = uow;
    }

    public async Task<ErrorOr<Unit>> Handle(DeleteAgentCommand m, CancellationToken ct)
    {
        var agent = await _agents.GetForUpdateAsync(m.Id, ct);
        if (agent is null)
            return AppErrors.Agents.NotFound(m.Id);
        if (await _terminals.AnyByAgentIdAsync(m.Id, ct))
            return AppErrors.Common.Validation("Невозможно удалить агента: существуют привязанные терминалы.");
        if (await _access.AnyByAgentIdAsync(m.Id, ct))
            return AppErrors.Common.Validation("Невозможно удалить агента: существуют права доступа (услуги или валюты).");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _agents.Remove(agent);
            return Task.FromResult(true);
        }, ct);

        return Unit.Value;
    }
}

