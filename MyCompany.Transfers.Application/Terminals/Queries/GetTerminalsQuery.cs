using MediatR;
using Microsoft.Extensions.Logging;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Models;
using MyCompany.Transfers.Application.Terminals.Commands;
using MyCompany.Transfers.Application.Terminals.Dtos;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Terminals.Queries;

public sealed record GetTerminalsQuery(int Page = 1, int PageSize = 10, string? Search = null) : IRequest<PagedResult<TerminalListDto>>;

public sealed class GetTerminalsQueryHandler : IRequestHandler<GetTerminalsQuery, PagedResult<TerminalListDto>>
{
    private readonly ILogger<GetTerminalsQueryHandler> _logger;
    private readonly ITerminalRepository _terminals;

    public GetTerminalsQueryHandler(ITerminalRepository terminals, ILogger<GetTerminalsQueryHandler> logger)
    {
        _terminals = terminals;
        _logger = logger;
    }

    public async Task<PagedResult<TerminalListDto>> Handle(GetTerminalsQuery request, CancellationToken ct)
    {
		try
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
            foreach (var item in list)
            {
                Console.WriteLine(item.ApiKey);
            }
            return new PagedResult<TerminalListDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }
		catch (Exception ex)
		{
            _logger.LogError($"Ошибка при получении списка терминалов. {ex}");
            throw new Exception("Ошибка при получении списка терминалов.");
        }
    }
}
