using MediatR;
using MyCompany.Transfers.Application.Services.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Models;

namespace MyCompany.Transfers.Application.Services.Queries;

public sealed record GetServicesQuery(int Page = 1, int PageSize = 10, string? Search = null) : IRequest<PagedResult<ServiceAdminDto>>;

public sealed class GetServicesQueryHandler : IRequestHandler<GetServicesQuery, PagedResult<ServiceAdminDto>>
{
    private readonly IServiceRepository _services;

    public GetServicesQueryHandler(IServiceRepository services) => _services = services;

    public async Task<PagedResult<ServiceAdminDto>> Handle(GetServicesQuery request, CancellationToken ct)
    {
        var list = await _services.GetAllAsync(ct);
        var dtos = list.Select(ServiceAdminDto.FromDomain).ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var q = request.Search.Trim();
            dtos = dtos.Where(s =>
                (s.Id?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.ProviderServiceId?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.ProviderId?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }

        var total = dtos.Count;
        var page = Math.Max(1, request.Page);
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);
        var items = dtos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<ServiceAdminDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
