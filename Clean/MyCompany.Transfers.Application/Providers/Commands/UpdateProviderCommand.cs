using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Providers.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Providers;

namespace MyCompany.Transfers.Application.Providers.Commands;

public sealed record UpdateProviderCommand(
    string Id,
    string? Name,
    string? BaseUrl,
    int? TimeoutSeconds,
    ProviderAuthType? AuthType,
    string? SettingsJson,
    bool? IsEnabled,
    int? FeePermille) : IRequest<ErrorOr<ProviderAdminDto>>;

public sealed class UpdateProviderCommandHandler : IRequestHandler<UpdateProviderCommand, ErrorOr<ProviderAdminDto>>
{
    private readonly IProviderRepository _providers;
    private readonly IUnitOfWork _uow;

    public UpdateProviderCommandHandler(IProviderRepository providers, IUnitOfWork uow)
    {
        _providers = providers;
        _uow = uow;
    }

    public async Task<ErrorOr<ProviderAdminDto>> Handle(UpdateProviderCommand cmd, CancellationToken ct)
    {
        var provider = await _providers.GetForUpdateAsync(cmd.Id, ct);
        if (provider is null)
            return AppErrors.Common.NotFound($"Провайдер '{cmd.Id}' не найден.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            if (cmd.Name is not null)
                provider.GetType().GetProperty("Name")!.SetValue(provider, cmd.Name);
            if (cmd.BaseUrl is not null)
                provider.GetType().GetProperty("BaseUrl")!.SetValue(provider, cmd.BaseUrl);
            if (cmd.TimeoutSeconds.HasValue)
                provider.GetType().GetProperty("TimeoutSeconds")!.SetValue(provider, cmd.TimeoutSeconds.Value);
            if (cmd.AuthType.HasValue)
                provider.GetType().GetProperty("AuthType")!.SetValue(provider, cmd.AuthType.Value);
            if (cmd.SettingsJson is not null)
                provider.UpdateSettings(cmd.SettingsJson);
            if (cmd.IsEnabled.HasValue)
            {
                if (cmd.IsEnabled.Value)
                    provider.Enable();
                else
                    provider.Disable();
            }
            if (cmd.FeePermille.HasValue)
                provider.GetType().GetProperty("FeePermille")!.SetValue(provider, cmd.FeePermille.Value);

            _providers.Update(provider);
            return Task.FromResult(true);
        }, ct);

        return ProviderAdminDto.FromDomain(provider);
    }
}
