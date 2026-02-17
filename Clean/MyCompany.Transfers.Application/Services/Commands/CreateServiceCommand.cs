using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Services.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Services.Commands;

public sealed record CreateServiceCommand(
    string Id,
    string ProviderId,
    string ProviderServiceId,
    string Name,
    string[] AllowedCurrencies,
    string? FxRounding,
    long MinAmountMinor,
    long MaxAmountMinor,
    Guid AccountDefinitionId,
    List<ServiceParamDefinitionDto> Parameters) : IRequest<ErrorOr<ServiceAdminDto>>;

public sealed class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, ErrorOr<ServiceAdminDto>>
{
    private readonly IServiceRepository _services;
    private readonly IUnitOfWork _uow;

    public CreateServiceCommandHandler(IServiceRepository services, IUnitOfWork uow)
    {
        _services = services;
        _uow = uow;
    }

    public async Task<ErrorOr<ServiceAdminDto>> Handle(CreateServiceCommand cmd, CancellationToken ct)
    {
        if (await _services.ExistsAsync(cmd.Id, ct))
            return AppErrors.Common.Validation($"Услуга '{cmd.Id}' уже существует.");

        var parameters = (cmd.Parameters ?? new List<ServiceParamDefinitionDto>())
            .Select(p => new ServiceParamDefinition(cmd.Id, p.ParameterId, p.Required))
            .ToList();

        var service = new Service(
            cmd.Id,
            cmd.ProviderId,
            cmd.Name,
            cmd.AllowedCurrencies ?? Array.Empty<string>(),
            cmd.MinAmountMinor,
            cmd.MaxAmountMinor,
            cmd.FxRounding,
            parameters);

        service.GetType().GetProperty("ProviderServiceId")!.SetValue(service, cmd.ProviderServiceId ?? cmd.Id);
        service.GetType().GetProperty("AccountDefinitionId")!.SetValue(service, cmd.AccountDefinitionId);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _services.Add(service);
            return Task.FromResult(true);
        }, ct);

        return ServiceAdminDto.FromDomain(service);
    }
}
