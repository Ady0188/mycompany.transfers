using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Services.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Services.Commands;

/// <summary>
/// Создание услуги с генерацией 9-значного Id на API. Используются первый провайдер и первое определение счёта.
/// </summary>
public sealed record CreateServiceSimpleCommand(string Name, string? Code) : IRequest<ErrorOr<ServiceAdminDto>>;

public sealed class CreateServiceSimpleCommandHandler : IRequestHandler<CreateServiceSimpleCommand, ErrorOr<ServiceAdminDto>>
{
    private readonly IServiceRepository _services;
    private readonly IProviderRepository _providers;
    private readonly IAccountDefinitionRepository _accountDefinitions;
    private readonly IUnitOfWork _uow;

    public CreateServiceSimpleCommandHandler(
        IServiceRepository services,
        IProviderRepository providers,
        IAccountDefinitionRepository accountDefinitions,
        IUnitOfWork uow)
    {
        _services = services;
        _providers = providers;
        _accountDefinitions = accountDefinitions;
        _uow = uow;
    }

    public async Task<ErrorOr<ServiceAdminDto>> Handle(CreateServiceSimpleCommand cmd, CancellationToken ct)
    {
        var id = await GenerateUnique9DigitIdAsync(ct);

        var providers = await _providers.GetAllAsync(ct);
        var firstProvider = providers.FirstOrDefault();
        if (firstProvider is null)
            return AppErrors.Common.Validation("Нет ни одного провайдера. Создайте провайдера перед добавлением услуги.");

        var accountDefs = await _accountDefinitions.GetAllAsync(ct);
        var firstAccountDef = accountDefs.FirstOrDefault();
        if (firstAccountDef is null)
            return AppErrors.Common.Validation("Нет ни одного определения счёта. Создайте определение счёта перед добавлением услуги.");

        var providerServiceId = string.IsNullOrWhiteSpace(cmd.Code) ? id : cmd.Code.Trim();
        var service = Service.Create(
            id,
            firstProvider.Id,
            providerServiceId,
            cmd.Name.Trim(),
            Array.Empty<string>(),
            0L,
            0L,
            null,
            firstAccountDef.Id,
            Array.Empty<ServiceParamDefinition>());

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
