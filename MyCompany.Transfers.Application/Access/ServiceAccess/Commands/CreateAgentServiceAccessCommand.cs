using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Access.ServiceAccess.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Access.ServiceAccess.Commands;

public sealed record CreateAgentServiceAccessCommand(
    string AgentId,
    string ServiceId,
    bool Enabled = true,
    int FeePermille = 0,
    long FeeFlatMinor = 0) : IRequest<ErrorOr<AgentServiceAccessAdminDto>>;

public sealed class CreateAgentServiceAccessCommandHandler : IRequestHandler<CreateAgentServiceAccessCommand, ErrorOr<AgentServiceAccessAdminDto>>
{
    private readonly IAccessRepository _access;
    private readonly IAgentReadRepository _agents;
    private readonly IServiceRepository _services;
    private readonly IUnitOfWork _uow;

    public CreateAgentServiceAccessCommandHandler(
        IAccessRepository access,
        IAgentReadRepository agents,
        IServiceRepository services,
        IUnitOfWork uow)
    {
        _access = access;
        _agents = agents;
        _services = services;
        _uow = uow;
    }

    public async Task<ErrorOr<AgentServiceAccessAdminDto>> Handle(CreateAgentServiceAccessCommand cmd, CancellationToken ct)
    {
        if (!await _agents.ExistsAsync(cmd.AgentId, ct))
            return AppErrors.Common.Validation($"Агент '{cmd.AgentId}' не найден.");
        if (!await _services.ExistsAsync(cmd.ServiceId, ct))
            return AppErrors.Common.Validation($"Услуга '{cmd.ServiceId}' не найдена.");
        if (await _access.ExistsAgentServiceAccessAsync(cmd.AgentId, cmd.ServiceId, ct))
            return AppErrors.Common.Validation($"Доступ агент–услуга ({cmd.AgentId}, {cmd.ServiceId}) уже существует.");

        var entity = AgentServiceAccess.Create(cmd.AgentId, cmd.ServiceId, cmd.Enabled, cmd.FeePermille, cmd.FeeFlatMinor);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _access.Add(entity);
            return Task.FromResult(true);
        }, ct);

        return AgentServiceAccessAdminDto.FromDomain(entity);
    }
}
