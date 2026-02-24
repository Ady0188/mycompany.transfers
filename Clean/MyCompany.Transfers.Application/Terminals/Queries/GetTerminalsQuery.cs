using MediatR;
using MyCompany.Transfers.Application.Terminals.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Models;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Terminals.Queries;

public sealed record GetTerminalsQuery(int Page = 1, int PageSize = 10, string? Search = null) : IRequest<PagedResult<TerminalListDto>>;

public sealed class GetTerminalsQueryHandler : IRequestHandler<GetTerminalsQuery, PagedResult<TerminalListDto>>
{
    private readonly ITerminalRepository _terminals;

    public GetTerminalsQueryHandler(ITerminalRepository terminals) => _terminals = terminals;

    public async Task<PagedResult<TerminalListDto>> Handle(GetTerminalsQuery request, CancellationToken ct)
    {
        var list = await _terminals.GetAllAsync(ct);
        IEnumerable<Terminal> filtered = list;
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var q = request.Search.Trim();
            filtered = list.Where(t =>
                (t.Id?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.AgentId?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.ApiKey?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        var allList = filtered.Select(TerminalListDto.FromDomain).ToList();

        var total = allList.Count;
        var page = Math.Max(1, request.Page);
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);
        var items = allList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<TerminalListDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
