using MediatR;
using MyCompany.Transfers.Application.Agents.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Models;

namespace MyCompany.Transfers.Application.Agents.Queries;

public sealed record GetAgentsQuery(int Page = 1, int PageSize = 10, string? Search = null) : IRequest<PagedResult<AgentAdminDto>>;

public sealed class GetAgentsQueryHandler : IRequestHandler<GetAgentsQuery, PagedResult<AgentAdminDto>>
{
    private readonly IAgentRepository _agents;

    public GetAgentsQueryHandler(IAgentRepository agents) => _agents = agents;

    public async Task<PagedResult<AgentAdminDto>> Handle(GetAgentsQuery request, CancellationToken ct)
    {
        var list = await _agents.GetAllAsync(ct);
        var dtos = list.Select(AgentAdminDto.FromDomain).ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var q = request.Search.Trim();
            dtos = dtos.Where(a =>
                (a.Id?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Account?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.TimeZoneId?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }

        var total = dtos.Count;
        var page = Math.Max(1, request.Page);
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);
        var items = dtos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<AgentAdminDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}

