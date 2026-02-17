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
            if (cmd.ProviderId is not null)
                service.GetType().GetProperty("ProviderId")!.SetValue(service, cmd.ProviderId);
            if (cmd.ProviderServiceId is not null)
                service.GetType().GetProperty("ProviderServiceId")!.SetValue(service, cmd.ProviderServiceId);
            if (cmd.Name is not null)
                service.GetType().GetProperty("Name")!.SetValue(service, cmd.Name);
            if (cmd.AllowedCurrencies is not null)
                service.GetType().GetProperty("AllowedCurrencies")!.SetValue(service, cmd.AllowedCurrencies);
            if (cmd.FxRounding is not null)
                service.GetType().GetProperty("FxRounding")!.SetValue(service, cmd.FxRounding);
            if (cmd.MinAmountMinor.HasValue)
                service.GetType().GetProperty("MinAmountMinor")!.SetValue(service, cmd.MinAmountMinor.Value);
            if (cmd.MaxAmountMinor.HasValue)
                service.GetType().GetProperty("MaxAmountMinor")!.SetValue(service, cmd.MaxAmountMinor.Value);
            if (cmd.AccountDefinitionId.HasValue)
                service.GetType().GetProperty("AccountDefinitionId")!.SetValue(service, cmd.AccountDefinitionId.Value);
            if (cmd.Parameters is not null)
            {
                service.Parameters.Clear();
                foreach (var p in cmd.Parameters)
                    service.Parameters.Add(new ServiceParamDefinition(service.Id, p.ParameterId, p.Required));
            }
            _services.Update(service);
            return Task.FromResult(true);
        }, ct);

        return ServiceAdminDto.FromDomain(service);
    }
}
