using MediatR;
using MyCompany.Transfers.Application.Bins.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Models;

namespace MyCompany.Transfers.Application.Bins.Queries;

public sealed record GetBinsQuery(int Page = 1, int PageSize = 10, string? Search = null) : IRequest<PagedResult<BinAdminDto>>;

public sealed class GetBinsQueryHandler : IRequestHandler<GetBinsQuery, PagedResult<BinAdminDto>>
{
    private readonly IBinRepository _bins;

    public GetBinsQueryHandler(IBinRepository bins) => _bins = bins;

    public async Task<PagedResult<BinAdminDto>> Handle(GetBinsQuery request, CancellationToken ct)
    {
        var list = await _bins.GetAllForAdminAsync(ct);
        var dtos = list.Select(BinAdminDto.FromDomain).ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var q = request.Search.Trim();
            dtos = dtos.Where(b =>
                (b.Prefix?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (b.Code?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (b.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }

        var total = dtos.Count;
        var page = Math.Max(1, request.Page);
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);
        var items = dtos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<BinAdminDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
