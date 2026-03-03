using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Terminals.Commands;

public sealed record DeleteTerminalCommand(string Id) : IRequest<ErrorOr<Success>>;

public sealed class DeleteTerminalCommandHandler : IRequestHandler<DeleteTerminalCommand, ErrorOr<Success>>
{
    private readonly ITerminalRepository _terminals;
    private readonly IUnitOfWork _uow;

    public DeleteTerminalCommandHandler(ITerminalRepository terminals, IUnitOfWork uow)
    {
        _terminals = terminals;
        _uow = uow;
    }

    public async Task<ErrorOr<Success>> Handle(DeleteTerminalCommand cmd, CancellationToken ct)
    {
        var terminal = await _terminals.GetForUpdateAsync(cmd.Id, ct);
        if (terminal is null)
            return AppErrors.Common.NotFound($"Терминал '{cmd.Id}' не найден.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _terminals.Remove(terminal);
            return Task.FromResult(true);
        }, ct);

        return Result.Success;
    }
}
