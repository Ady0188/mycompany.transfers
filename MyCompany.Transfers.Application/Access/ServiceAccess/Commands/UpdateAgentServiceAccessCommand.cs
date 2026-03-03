using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Access.ServiceAccess.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Access.ServiceAccess.Commands;

public sealed record UpdateAgentServiceAccessCommand(
    string AgentId,
    string ServiceId,
    bool? Enabled = null,
    int? FeePermille = null,
    long? FeeFlatMinor = null) : IRequest<ErrorOr<AgentServiceAccessAdminDto>>;

public sealed class UpdateAgentServiceAccessCommandHandler : IRequestHandler<UpdateAgentServiceAccessCommand, ErrorOr<AgentServiceAccessAdminDto>>
{
    private readonly IAccessRepository _access;
    private readonly IUnitOfWork _uow;

    public UpdateAgentServiceAccessCommandHandler(IAccessRepository access, IUnitOfWork uow)
    {
        _access = access;
        _uow = uow;
    }

    public async Task<ErrorOr<AgentServiceAccessAdminDto>> Handle(UpdateAgentServiceAccessCommand cmd, CancellationToken ct)
    {
        var entity = await _access.GetAgentServiceAccessForUpdateAsync(cmd.AgentId, cmd.ServiceId, ct);
        if (entity == null)
            return Error.NotFound(description: "Запись доступа агент–услуга не найдена.");

        entity.UpdateProfile(cmd.Enabled, cmd.FeePermille, cmd.FeeFlatMinor);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _access.Update(entity);
            return Task.FromResult(true);
        }, ct);

        return AgentServiceAccessAdminDto.FromDomain(entity);
    }
}
