using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Application.Providers.Commands;

public sealed record DeleteProviderCommand(string Id) : IRequest<ErrorOr<Success>>;

public sealed class DeleteProviderCommandHandler : IRequestHandler<DeleteProviderCommand, ErrorOr<Success>>
{
    private readonly IProviderRepository _providers;
    private readonly IUnitOfWork _uow;

    public DeleteProviderCommandHandler(IProviderRepository providers, IUnitOfWork uow)
    {
        _providers = providers;
        _uow = uow;
    }

    public async Task<ErrorOr<Success>> Handle(DeleteProviderCommand cmd, CancellationToken ct)
    {
        var provider = await _providers.GetForUpdateAsync(cmd.Id, ct);
        if (provider is null)
            return AppErrors.Common.NotFound($"Провайдер '{cmd.Id}' не найден.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _providers.Remove(provider);
            return Task.FromResult(true);
        }, ct);

        return Result.Success;
    }
}
