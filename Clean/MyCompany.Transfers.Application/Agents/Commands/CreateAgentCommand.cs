using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Agents.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Agents.Commands;

public sealed record CreateAgentCommand(
    string Id,
    string TimeZoneId,
    string SettingsJson) : IRequest<ErrorOr<AgentAdminDto>>;

public sealed class CreateAgentCommandHandler : IRequestHandler<CreateAgentCommand, ErrorOr<AgentAdminDto>>
{
    private readonly IAgentRepository _agents;
    private readonly IUnitOfWork _uow;

    public CreateAgentCommandHandler(IAgentRepository agents, IUnitOfWork uow)
    {
        _agents = agents;
        _uow = uow;
    }

    public async Task<ErrorOr<AgentAdminDto>> Handle(CreateAgentCommand m, CancellationToken ct)
    {
        if (await _agents.ExistsAsync(m.Id, ct))
            return AppErrors.Common.Validation($"Агент '{m.Id}' уже существует.");

        Agent agent = new(m.Id)
        {
            TimeZoneId = string.IsNullOrWhiteSpace(m.TimeZoneId) ? "Asia/Dushanbe" : m.TimeZoneId,
        };

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            if (!string.IsNullOrWhiteSpace(m.SettingsJson))
                agent.GetType().GetProperty("SettingsJson")!.SetValue(agent, m.SettingsJson);

            _agents.Add(agent);
            return Task.FromResult(true);
        }, ct);

        return AgentAdminDto.FromDomain(agent);
    }
}

