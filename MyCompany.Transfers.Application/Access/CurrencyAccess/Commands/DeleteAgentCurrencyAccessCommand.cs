using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Access.CurrencyAccess.Commands;

public sealed record DeleteAgentCurrencyAccessCommand(string AgentId, string Currency) : IRequest<ErrorOr<Success>>;

public sealed class DeleteAgentCurrencyAccessCommandHandler : IRequestHandler<DeleteAgentCurrencyAccessCommand, ErrorOr<Success>>
{
    private readonly IAccessRepository _access;
    private readonly IUnitOfWork _uow;

    public DeleteAgentCurrencyAccessCommandHandler(IAccessRepository access, IUnitOfWork uow)
    {
        _access = access;
        _uow = uow;
    }

    public async Task<ErrorOr<Success>> Handle(DeleteAgentCurrencyAccessCommand cmd, CancellationToken ct)
    {
        var entity = await _access.GetAgentCurrencyAccessForUpdateAsync(cmd.AgentId, cmd.Currency, ct);
        if (entity == null)
            return Error.NotFound(description: "Запись доступа агент–валюта не найдена.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _access.Remove(entity);
            return Task.FromResult(true);
        }, ct);

        return Result.Success;
    }
}
