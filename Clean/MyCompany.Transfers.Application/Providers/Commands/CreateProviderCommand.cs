using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Providers.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Providers;

namespace MyCompany.Transfers.Application.Providers.Commands;

public sealed record CreateProviderCommand(
    string Id,
    string Account,
    string Name,
    string BaseUrl,
    int TimeoutSeconds,
    ProviderAuthType AuthType,
    string SettingsJson,
    bool IsEnabled = true,
    int FeePermille = 0) : IRequest<ErrorOr<ProviderAdminDto>>;

public sealed class CreateProviderCommandHandler : IRequestHandler<CreateProviderCommand, ErrorOr<ProviderAdminDto>>
{
    private readonly IProviderRepository _providers;
    private readonly IUnitOfWork _uow;

    public CreateProviderCommandHandler(IProviderRepository providers, IUnitOfWork uow)
    {
        _providers = providers;
        _uow = uow;
    }

    public async Task<ErrorOr<ProviderAdminDto>> Handle(CreateProviderCommand cmd, CancellationToken ct)
    {
        if (await _providers.ExistsAsync(cmd.Id, ct))
            return AppErrors.Common.Validation($"Провайдер '{cmd.Id}' уже существует.");

        var provider = new Provider(
            cmd.Id,
            cmd.Account,
            cmd.Name,
            cmd.BaseUrl,
            cmd.TimeoutSeconds,
            cmd.AuthType,
            cmd.SettingsJson ?? "{}",
            cmd.IsEnabled,
            cmd.FeePermille);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _providers.Add(provider);
            return Task.FromResult(true);
        }, ct);

        return ProviderAdminDto.FromDomain(provider);
    }
}
