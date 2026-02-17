using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Services.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Services.Commands;

public sealed record UpdateServiceCommand(
    string Id,
    string? ProviderId,
    string? ProviderServiceId,
    string? Name,
    string[]? AllowedCurrencies,
    string? FxRounding,
    long? MinAmountMinor,
    long? MaxAmountMinor,
    Guid? AccountDefinitionId,
    List<ServiceParamDefinitionDto>? Parameters) : IRequest<ErrorOr<ServiceAdminDto>>;

public sealed class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand, ErrorOr<ServiceAdminDto>>
{
    private readonly IServiceRepository _services;
    private readonly IUnitOfWork _uow;

    public UpdateServiceCommandHandler(IServiceRepository services, IUnitOfWork uow)
    {
        _services = services;
        _uow = uow;
    }

    public async Task<ErrorOr<ServiceAdminDto>> Handle(UpdateServiceCommand cmd, CancellationToken ct)
    {
        var service = await _services.GetForUpdateAsync(cmd.Id, ct);
        if (service is null)
            return AppErrors.Common.NotFound($"Услуга '{cmd.Id}' не найдена.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            var newParams = cmd.Parameters?.Select(p => new ServiceParamDefinition(service.Id, p.ParameterId, p.Required)).ToList();
            service.UpdateProfile(cmd.ProviderId, cmd.ProviderServiceId, cmd.Name, cmd.AllowedCurrencies,
                cmd.MinAmountMinor, cmd.MaxAmountMinor, cmd.FxRounding, cmd.AccountDefinitionId, newParams);
            _services.Update(service);
            return Task.FromResult(true);
        }, ct);

        return ServiceAdminDto.FromDomain(service);
    }
}
