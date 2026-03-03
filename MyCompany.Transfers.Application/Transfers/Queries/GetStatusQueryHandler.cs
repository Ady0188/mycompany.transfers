using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Transfers.Queries;

public sealed class GetStatusQueryHandler : IRequestHandler<GetStatusQuery, ErrorOr<StatusResponseDto>>
{
    private readonly ITransferReadRepository _read;
    private readonly IAgentReadRepository _agents;
    private readonly ILogger<GetStatusQueryHandler> _logger;

    public GetStatusQueryHandler(ITransferReadRepository read, IAgentReadRepository agents, ILogger<GetStatusQueryHandler> logger)
    {
        _read = read;
        _agents = agents;
        _logger = logger;
    }

    public async Task<ErrorOr<StatusResponseDto>> Handle(GetStatusQuery m, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(m.ExternalId) && string.IsNullOrWhiteSpace(m.TransferId))
                return AppErrors.Common.Validation("Необходимо указать externalId или transferId.");

            var agent = await _agents.GetForUpdateAsync(m.AgentId, ct);
            if (agent is null)
            {
                _logger.LogInformation("GetStatusQueryHandler: AgentId={AgentId}, agent not found", m.AgentId);
                return AppErrors.Agents.NotFound(m.AgentId);
            }

            Transfer? trr;
            if (!string.IsNullOrWhiteSpace(m.ExternalId))
            {
                trr = await _read.GetStatusByExternalIdAsync(agent.Id, m.ExternalId!, ct);
                return trr is null ? AppErrors.Transfers.NotFound(m.ExternalId!) : trr.ToStatusResponseDto(agent);
            }

            if (!Guid.TryParse(m.TransferId, out var id))
                return AppErrors.Common.Validation("transferId имеет неверный формат (ожидается GUID).");

            trr = await _read.GetStatusByIdAsync(agent.Id, id, ct);
            return trr is null ? AppErrors.Transfers.NotFoundById(id) : trr.ToStatusResponseDto(agent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetStatusQueryHandler: AgentId={AgentId}, ExternalId={ExternalId}, TransferId={TransferId}", m.AgentId, m.ExternalId, m.TransferId);
            return AppErrors.Common.Unexpected();
        }
    }
}
