using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Services.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Services.Queries;

public sealed record GetServiceByIdAdminQuery(string ServiceId) : IRequest<ErrorOr<ServiceAdminDto>>;

public sealed class GetServiceByIdAdminQueryHandler : IRequestHandler<GetServiceByIdAdminQuery, ErrorOr<ServiceAdminDto>>
{
    private readonly IServiceRepository _services;

    public GetServiceByIdAdminQueryHandler(IServiceRepository services) => _services = services;

    public async Task<ErrorOr<ServiceAdminDto>> Handle(GetServiceByIdAdminQuery request, CancellationToken ct)
    {
        var service = await _services.GetByIdAsync(request.ServiceId, ct);
        if (service is null)
            return AppErrors.Common.NotFound($"Услуга '{request.ServiceId}' не найдена.");
        return ServiceAdminDto.FromDomain(service);
    }
}
