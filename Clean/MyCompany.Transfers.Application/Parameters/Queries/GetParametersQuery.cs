using MediatR;
using MyCompany.Transfers.Application.Parameters.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Models;

namespace MyCompany.Transfers.Application.Parameters.Queries;

public sealed record GetParametersQuery(int Page = 1, int PageSize = 10, string? Search = null) : IRequest<PagedResult<ParameterAdminDto>>;

public sealed class GetParametersQueryHandler : IRequestHandler<GetParametersQuery, PagedResult<ParameterAdminDto>>
{
    private readonly IParameterRepository _parameters;

    public GetParametersQueryHandler(IParameterRepository parameters) => _parameters = parameters;

    public async Task<PagedResult<ParameterAdminDto>> Handle(GetParametersQuery request, CancellationToken ct)
    {
        var list = await _parameters.GetAllForAdminAsync(ct);
        var dtos = list.Select(ParameterAdminDto.FromDomain).ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var q = request.Search.Trim();
            dtos = dtos.Where(p =>
                (p.Id?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Code?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }

        var total = dtos.Count;
        var page = Math.Max(1, request.Page);
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);
        var items = dtos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<ParameterAdminDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
