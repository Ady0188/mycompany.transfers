using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Access.ServiceAccess.Commands;

public sealed record DeleteAgentServiceAccessCommand(string AgentId, string ServiceId) : IRequest<ErrorOr<Success>>;

public sealed class DeleteAgentServiceAccessCommandHandler : IRequestHandler<DeleteAgentServiceAccessCommand, ErrorOr<Success>>
{
    private readonly IAccessRepository _access;
    private readonly IUnitOfWork _uow;

    public DeleteAgentServiceAccessCommandHandler(IAccessRepository access, IUnitOfWork uow)
    {
        _access = access;
        _uow = uow;
    }

    public async Task<ErrorOr<Success>> Handle(DeleteAgentServiceAccessCommand cmd, CancellationToken ct)
    {
        var entity = await _access.GetAgentServiceAccessForUpdateAsync(cmd.AgentId, cmd.ServiceId, ct);
        if (entity == null)
            return Error.NotFound(description: "Запись доступа агент–услуга не найдена.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _access.Remove(entity);
            return Task.FromResult(true);
        }, ct);

        return Result.Success;
    }
}
