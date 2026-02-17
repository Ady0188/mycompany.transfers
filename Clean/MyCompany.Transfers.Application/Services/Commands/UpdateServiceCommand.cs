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
    private readonly IProviderRepository _providers;
    private readonly IAccountDefinitionRepository _accountDefinitions;
    private readonly IParameterRepository _parameters;
    private readonly IUnitOfWork _uow;

    public UpdateServiceCommandHandler(
        IServiceRepository services,
        IProviderRepository providers,
        IAccountDefinitionRepository accountDefinitions,
        IParameterRepository parameters,
        IUnitOfWork uow)
    {
        _services = services;
        _providers = providers;
        _accountDefinitions = accountDefinitions;
        _parameters = parameters;
        _uow = uow;
    }

    public async Task<ErrorOr<ServiceAdminDto>> Handle(UpdateServiceCommand cmd, CancellationToken ct)
    {
        var service = await _services.GetForUpdateAsync(cmd.Id, ct);
        if (service is null)
            return AppErrors.Common.NotFound($"Услуга '{cmd.Id}' не найдена.");
        if (cmd.ProviderId is not null && !await _providers.ExistsAsync(cmd.ProviderId, ct))
            return AppErrors.Common.Validation($"Провайдер '{cmd.ProviderId}' не найден.");
        if (cmd.AccountDefinitionId.HasValue && await _accountDefinitions.GetAsync(cmd.AccountDefinitionId.Value, ct) is null)
            return AppErrors.Common.Validation($"Определение счёта с Id '{cmd.AccountDefinitionId}' не найдено.");
        if (cmd.Parameters is not null && cmd.Parameters.Count > 0)
        {
            var paramIds = cmd.Parameters.Select(p => p.ParameterId).Distinct().ToList();
            var paramMap = await _parameters.GetByIdsAsMapAsync(paramIds, ct);
            var missing = paramIds.Where(id => !paramMap.ContainsKey(id)).ToList();
            if (missing.Count > 0)
                return AppErrors.Common.Validation($"Параметр(ы) не найдены: {string.Join(", ", missing)}.");
        }

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
