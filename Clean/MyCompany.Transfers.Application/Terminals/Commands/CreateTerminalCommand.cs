using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Terminals.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Terminals.Commands;

public sealed record CreateTerminalCommand(
    string AgentId,
    string Name,
    string ApiKey,
    bool Active = true) : IRequest<ErrorOr<TerminalAdminDto>>;

public sealed class CreateTerminalCommandHandler : IRequestHandler<CreateTerminalCommand, ErrorOr<TerminalAdminDto>>
{
    private readonly ITerminalRepository _terminals;
    private readonly IAgentReadRepository _agents;
    private readonly IUnitOfWork _uow;

    public CreateTerminalCommandHandler(ITerminalRepository terminals, IAgentReadRepository agents, IUnitOfWork uow)
    {
        _terminals = terminals;
        _agents = agents;
        _uow = uow;
    }

    public async Task<ErrorOr<TerminalAdminDto>> Handle(CreateTerminalCommand cmd, CancellationToken ct)
    {
        if (!await _agents.ExistsAsync(cmd.AgentId, ct))
            return AppErrors.Common.Validation($"Агент '{cmd.AgentId}' не найден. Создание терминала невозможно.");

        var id = Guid.NewGuid().ToString("N");
        var secret = SecretGenerator.GenerateTerminalSecret();
        var terminal = Terminal.Create(id, cmd.AgentId, cmd.Name, cmd.ApiKey, secret, cmd.Active);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _terminals.Add(terminal);
            return Task.FromResult(true);
        }, ct);

        return TerminalAdminDto.FromDomain(terminal, maskSecret: true);
    }
}
