using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Terminals.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Terminals.Commands;

public sealed record CreateTerminalCommand(
    string Id,
    string AgentId,
    string Name,
    string ApiKey,
    string Secret,
    bool Active = true) : IRequest<ErrorOr<TerminalAdminDto>>;

public sealed class CreateTerminalCommandHandler : IRequestHandler<CreateTerminalCommand, ErrorOr<TerminalAdminDto>>
{
    private readonly ITerminalRepository _terminals;
    private readonly IUnitOfWork _uow;

    public CreateTerminalCommandHandler(ITerminalRepository terminals, IUnitOfWork uow)
    {
        _terminals = terminals;
        _uow = uow;
    }

    public async Task<ErrorOr<TerminalAdminDto>> Handle(CreateTerminalCommand cmd, CancellationToken ct)
    {
        if (await _terminals.ExistsAsync(cmd.Id, ct))
            return AppErrors.Common.Validation($"Терминал '{cmd.Id}' уже существует.");

        var terminal = Terminal.Create(cmd.Id, cmd.AgentId, cmd.Name, cmd.ApiKey, cmd.Secret ?? "", cmd.Active);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _terminals.Add(terminal);
            return Task.FromResult(true);
        }, ct);

        return TerminalAdminDto.FromDomain(terminal);
    }
}
