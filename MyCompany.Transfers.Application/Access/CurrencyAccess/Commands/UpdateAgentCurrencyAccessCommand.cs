using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Access.CurrencyAccess.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Access.CurrencyAccess.Commands;

public sealed record UpdateAgentCurrencyAccessCommand(
    string AgentId,
    string Currency,
    bool? Enabled = null) : IRequest<ErrorOr<AgentCurrencyAccessAdminDto>>;

public sealed class UpdateAgentCurrencyAccessCommandHandler : IRequestHandler<UpdateAgentCurrencyAccessCommand, ErrorOr<AgentCurrencyAccessAdminDto>>
{
    private readonly IAccessRepository _access;
    private readonly IUnitOfWork _uow;

    public UpdateAgentCurrencyAccessCommandHandler(IAccessRepository access, IUnitOfWork uow)
    {
        _access = access;
        _uow = uow;
    }

    public async Task<ErrorOr<AgentCurrencyAccessAdminDto>> Handle(UpdateAgentCurrencyAccessCommand cmd, CancellationToken ct)
    {
        var entity = await _access.GetAgentCurrencyAccessForUpdateAsync(cmd.AgentId, cmd.Currency, ct);
        if (entity == null)
            return Error.NotFound(description: "Запись доступа агент–валюта не найдена.");

        entity.UpdateProfile(cmd.Enabled);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _access.Update(entity);
            return Task.FromResult(true);
        }, ct);

        return AgentCurrencyAccessAdminDto.FromDomain(entity);
    }
}
