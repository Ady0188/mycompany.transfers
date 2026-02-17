using MediatR;
using MyCompany.Transfers.Application.Services.Dtos;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Services.Queries;

public sealed record GetServicesQuery() : IRequest<IReadOnlyList<ServiceAdminDto>>;

public sealed class GetServicesQueryHandler : IRequestHandler<GetServicesQuery, IReadOnlyList<ServiceAdminDto>>
{
    private readonly IServiceRepository _services;

    public GetServicesQueryHandler(IServiceRepository services) => _services = services;

    public async Task<IReadOnlyList<ServiceAdminDto>> Handle(GetServicesQuery request, CancellationToken ct)
    {
        var list = await _services.GetAllAsync(ct);
        return list.Select(ServiceAdminDto.FromDomain).ToList();
    }
}
