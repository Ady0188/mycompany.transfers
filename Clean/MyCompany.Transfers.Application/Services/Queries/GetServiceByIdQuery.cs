using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Services.Queries;

public sealed record GetServiceByIdQuery(string ServiceId) : IRequest<ErrorOr<(Service Service, bool IsByPan)>>;

public sealed class GetServiceByIdQueryHandler : IRequestHandler<GetServiceByIdQuery, ErrorOr<(Service Service, bool IsByPan)>>
{
    private readonly IServiceRepository _services;

    public GetServiceByIdQueryHandler(IServiceRepository services) => _services = services;

    public async Task<ErrorOr<(Service Service, bool IsByPan)>> Handle(GetServiceByIdQuery request, CancellationToken ct)
    {
        var (service, isByPan) = await _services.GetByIdWithTypeAsync(request.ServiceId, ct);
        if (service is null)
            return Error.NotFound("Service.NotFound", $"Service '{request.ServiceId}' not found.");
        return (service, isByPan);
    }
}
