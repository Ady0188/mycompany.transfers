using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Agents.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Agents.Commands;

public sealed record UpdateAgentCommand(
    string Id,
    string TimeZoneId,
    string SettingsJson) : IRequest<ErrorOr<AgentAdminDto>>;

public sealed class UpdateAgentCommandHandler : IRequestHandler<UpdateAgentCommand, ErrorOr<AgentAdminDto>>
{
    private readonly IAgentRepository _agents;
    private readonly IUnitOfWork _uow;

    public UpdateAgentCommandHandler(IAgentRepository agents, IUnitOfWork uow)
    {
        _agents = agents;
        _uow = uow;
    }

    public async Task<ErrorOr<AgentAdminDto>> Handle(UpdateAgentCommand m, CancellationToken ct)
    {
        var agent = await _agents.GetForUpdateAsync(m.Id, ct);
        if (agent is null)
            return AppErrors.Agents.NotFound(m.Id);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            agent.TimeZoneId = string.IsNullOrWhiteSpace(m.TimeZoneId) ? agent.TimeZoneId : m.TimeZoneId;
            if (!string.IsNullOrWhiteSpace(m.SettingsJson))
                agent.GetType().GetProperty("SettingsJson")!.SetValue(agent, m.SettingsJson);

            _agents.Update(agent);
            return Task.FromResult(true);
        }, ct);

        return AgentAdminDto.FromDomain(agent);
    }
}

