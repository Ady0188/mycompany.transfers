using ErrorOr;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;
using MediatR;

namespace MyCompany.Transfers.Application.Services.Queries;

public record GetServiceByIdQuery(string serviceId) : IRequest<ErrorOr<(Service Service, bool IsByPan)>>;
public class GetServiceByIdQueryHandler : IRequestHandler<GetServiceByIdQuery, ErrorOr<(Service Service, bool IsByPan)>>
{
    private readonly IServiceRepository _serviceRepository;

    public GetServiceByIdQueryHandler(IServiceRepository serviceRepository)
    {
        _serviceRepository = serviceRepository;
    }

    public async Task<ErrorOr<(Service Service, bool IsByPan)>> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
    {
        var service = await _serviceRepository.GetByIdWithTypeAsync(request.serviceId, cancellationToken);

        if (service.Service is null)
            return Error.NotFound(
                code: "Service.NotFound",
                description: $"Service with ID '{request.serviceId}' was not found."
            );

        return service;
    }
}