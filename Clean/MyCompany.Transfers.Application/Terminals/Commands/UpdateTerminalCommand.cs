using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Terminals.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Terminals.Commands;

public sealed record UpdateTerminalCommand(
    string Id,
    string? AgentId,
    string? Name,
    string? ApiKey,
    string? Secret,
    bool? Active) : IRequest<ErrorOr<TerminalAdminDto>>;

public sealed class UpdateTerminalCommandHandler : IRequestHandler<UpdateTerminalCommand, ErrorOr<TerminalAdminDto>>
{
    private readonly ITerminalRepository _terminals;
    private readonly IUnitOfWork _uow;

    public UpdateTerminalCommandHandler(ITerminalRepository terminals, IUnitOfWork uow)
    {
        _terminals = terminals;
        _uow = uow;
    }

    public async Task<ErrorOr<TerminalAdminDto>> Handle(UpdateTerminalCommand cmd, CancellationToken ct)
    {
        var terminal = await _terminals.GetForUpdateAsync(cmd.Id, ct);
        if (terminal is null)
            return AppErrors.Common.NotFound($"Терминал '{cmd.Id}' не найден.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            terminal.UpdateProfile(cmd.AgentId, cmd.Name, cmd.ApiKey, cmd.Secret, cmd.Active);
            _terminals.Update(terminal);
            return Task.FromResult(true);
        }, ct);

        return TerminalAdminDto.FromDomain(terminal);
    }
}
