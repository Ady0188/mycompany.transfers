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
    private readonly IProviderRepository _providers;
    private readonly IAccountDefinitionRepository _accountDefinitions;
    private readonly IParameterRepository _parameters;
    private readonly IUnitOfWork _uow;

    public CreateServiceCommandHandler(
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

    public async Task<ErrorOr<ServiceAdminDto>> Handle(CreateServiceCommand cmd, CancellationToken ct)
    {
        var resolvedId = string.IsNullOrWhiteSpace(cmd.Id)
            ? await GenerateUnique9DigitIdAsync(ct)
            : cmd.Id.Trim();

        if (await _services.ExistsAsync(resolvedId, ct))
            return AppErrors.Common.Validation($"Услуга '{resolvedId}' уже существует.");

        if (string.IsNullOrWhiteSpace(cmd.ProviderId))
            return AppErrors.Common.Validation("ProviderId обязателен.");
        if (!await _providers.ExistsAsync(cmd.ProviderId, ct))
            return AppErrors.Common.Validation($"Провайдер '{cmd.ProviderId}' не найден.");

        if (cmd.AccountDefinitionId == Guid.Empty)
            return AppErrors.Common.Validation("AccountDefinitionId обязателен.");
        if (await _accountDefinitions.GetAsync(cmd.AccountDefinitionId, ct) is null)
            return AppErrors.Common.Validation($"Определение счёта с Id '{cmd.AccountDefinitionId}' не найдено.");

        var paramIds = (cmd.Parameters ?? new List<ServiceParamDefinitionDto>()).Select(p => p.ParameterId).Distinct().ToList();
        if (paramIds.Count > 0)
        {
            var paramMap = await _parameters.GetByIdsAsMapAsync(paramIds, ct);
            var missing = paramIds.Where(id => !paramMap.ContainsKey(id)).ToList();
            if (missing.Count > 0)
                return AppErrors.Common.Validation($"Параметр(ы) не найдены: {string.Join(", ", missing)}.");
        }

        var parameters = (cmd.Parameters ?? new List<ServiceParamDefinitionDto>())
            .Select(p => new ServiceParamDefinition(resolvedId, p.ParameterId, p.Required))
            .ToList();

        var service = Service.Create(
            resolvedId,
            cmd.ProviderId,
            cmd.ProviderServiceId ?? resolvedId,
            cmd.Name,
            cmd.AllowedCurrencies ?? Array.Empty<string>(),
            cmd.MinAmountMinor,
            cmd.MaxAmountMinor,
            cmd.FxRounding,
            cmd.AccountDefinitionId,
            parameters);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _services.Add(service);
            return Task.FromResult(true);
        }, ct);

        return ServiceAdminDto.FromDomain(service);
    }

    private async Task<string> GenerateUnique9DigitIdAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var id = Random.Shared.Next(100_000_000, 1_000_000_000).ToString();
            if (!await _services.ExistsAsync(id, ct))
                return id;
        }
        return Guid.NewGuid().ToString("N")[..9];
    }
}
