using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Agents.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Agents.Queries;

public sealed record GetSentCredentialsHistoryQuery(string AgentId) : IRequest<ErrorOr<IReadOnlyList<SentCredentialsEmailItemDto>>>;

public sealed class GetSentCredentialsHistoryQueryHandler : IRequestHandler<GetSentCredentialsHistoryQuery, ErrorOr<IReadOnlyList<SentCredentialsEmailItemDto>>>
{
    private readonly IAgentReadRepository _agents;
    private readonly ISentCredentialsEmailRepository _sentHistory;

    public GetSentCredentialsHistoryQueryHandler(IAgentReadRepository agents, ISentCredentialsEmailRepository sentHistory)
    {
        _agents = agents;
        _sentHistory = sentHistory;
    }

    public async Task<ErrorOr<IReadOnlyList<SentCredentialsEmailItemDto>>> Handle(GetSentCredentialsHistoryQuery request, CancellationToken ct)
    {
        if (!await _agents.ExistsAsync(request.AgentId, ct))
            return AppErrors.Agents.NotFound(request.AgentId);

        var list = await _sentHistory.GetByAgentIdAsync(request.AgentId, ct);
        var dtos = list.Select(e => new SentCredentialsEmailItemDto(e.Id, e.TerminalId, e.ToEmail, e.Subject, e.SentAtUtc)).ToList();
        return dtos;
    }
}
