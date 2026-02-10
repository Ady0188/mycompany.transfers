using ErrorOr;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Common;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Domain.Transfers.Dtos;
using MediatR;
using NLog;

namespace MyCompany.Transfers.Application.MyCompanyTransfers.Queries;

public sealed record GetStatusQuery(
    string AgentId,
    string? ExternalId,
    string? TransferId
) : IRequest<ErrorOr<StatusResponseDto>>;

public sealed class GetStatusQueryHandler : IRequestHandler<GetStatusQuery, ErrorOr<StatusResponseDto>>
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly ITransferReadRepository _read;
    private readonly IAgentReadRepository _agents;

    public GetStatusQueryHandler(ITransferReadRepository read, IAgentReadRepository agents)
    {
        _read = read;
        _agents = agents;
    }

    public async Task<ErrorOr<StatusResponseDto>> Handle(GetStatusQuery m, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(m.ExternalId) && string.IsNullOrWhiteSpace(m.TransferId))
                return AppErrors.Common.Validation("Необходимо указать externalId или transferId.");

            Transfer? trr;

            var agent = await _agents.GetForUpdateAsync(m.AgentId, ct);
            if (agent is null)
            {
                _logger.Info($"GetStatusQueryHandler.Handle: AgentId={m.AgentId}, agent not found");
                return AppErrors.Agents.NotFound(m.AgentId);
            }

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
            _logger.Error($"GetStatusQueryHandler.Handle: AgentId={m.AgentId}, ExternalId={m.ExternalId}, TransferId={m.TransferId} unexpected error: {ex}");
            return AppErrors.Common.Unexpected();
        }
    }
}