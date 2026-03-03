using MediatR;
using MyCompany.Transfers.Application.Providers.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Models;

namespace MyCompany.Transfers.Application.Providers.Queries;

public sealed record GetProvidersQuery(int Page = 1, int PageSize = 10, string? Search = null) : IRequest<PagedResult<ProviderAdminDto>>;

public sealed class GetProvidersQueryHandler : IRequestHandler<GetProvidersQuery, PagedResult<ProviderAdminDto>>
{
    private readonly IProviderRepository _providers;

    public GetProvidersQueryHandler(IProviderRepository providers) => _providers = providers;

    public async Task<PagedResult<ProviderAdminDto>> Handle(GetProvidersQuery request, CancellationToken ct)
    {
        var list = await _providers.GetAllAsync(ct);
        var dtos = list.Select(ProviderAdminDto.FromDomain).ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var q = request.Search.Trim();
            dtos = dtos.Where(p =>
                (p.Id?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Account?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.BaseUrl?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }

        var total = dtos.Count;
        var page = Math.Max(1, request.Page);
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);
        var items = dtos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<ProviderAdminDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
