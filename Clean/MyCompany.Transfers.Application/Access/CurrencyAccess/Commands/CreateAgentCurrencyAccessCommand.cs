using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Access.CurrencyAccess.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Access.CurrencyAccess.Commands;

public sealed record CreateAgentCurrencyAccessCommand(
    string AgentId,
    string Currency,
    bool Enabled = true) : IRequest<ErrorOr<AgentCurrencyAccessAdminDto>>;

public sealed class CreateAgentCurrencyAccessCommandHandler : IRequestHandler<CreateAgentCurrencyAccessCommand, ErrorOr<AgentCurrencyAccessAdminDto>>
{
    private readonly IAccessRepository _access;
    private readonly IAgentReadRepository _agents;
    private readonly IUnitOfWork _uow;

    public CreateAgentCurrencyAccessCommandHandler(
        IAccessRepository access,
        IAgentReadRepository agents,
        IUnitOfWork uow)
    {
        _access = access;
        _agents = agents;
        _uow = uow;
    }

    public async Task<ErrorOr<AgentCurrencyAccessAdminDto>> Handle(CreateAgentCurrencyAccessCommand cmd, CancellationToken ct)
    {
        if (!await _agents.ExistsAsync(cmd.AgentId, ct))
            return AppErrors.Common.Validation($"Агент '{cmd.AgentId}' не найден.");
        if (await _access.ExistsAgentCurrencyAccessAsync(cmd.AgentId, cmd.Currency, ct))
            return AppErrors.Common.Validation($"Доступ агент–валюта ({cmd.AgentId}, {cmd.Currency}) уже существует.");

        var entity = AgentCurrencyAccess.Create(cmd.AgentId, cmd.Currency, cmd.Enabled);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _access.Add(entity);
            return Task.FromResult(true);
        }, ct);

        return AgentCurrencyAccessAdminDto.FromDomain(entity);
    }
}
