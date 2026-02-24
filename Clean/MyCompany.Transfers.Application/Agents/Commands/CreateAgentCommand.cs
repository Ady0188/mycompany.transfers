using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Agents.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Agents.Commands;

public sealed record CreateAgentCommand(
    string Id,
    string Account,
    string? Name,
    string TimeZoneId,
    string SettingsJson,
    string? PartnerEmail = null) : IRequest<ErrorOr<AgentAdminDto>>;

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

        Agent agent = Agent.Create(m.Id, m.Account, m.Name, m.TimeZoneId, m.SettingsJson, m.PartnerEmail);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _agents.Add(agent);
            return Task.FromResult(true);
        }, ct);

        return AgentAdminDto.FromDomain(agent);
    }
}

