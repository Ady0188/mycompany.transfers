using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Bins;

namespace MyCompany.Transfers.Application.Bins.Commands;

public sealed record DeleteBinCommand(Guid Id) : IRequest<ErrorOr<Success>>;

public sealed class DeleteBinCommandHandler : IRequestHandler<DeleteBinCommand, ErrorOr<Success>>
{
    private readonly IBinRepository _bins;
    private readonly IUnitOfWork _uow;

    public DeleteBinCommandHandler(IBinRepository bins, IUnitOfWork uow)
    {
        _bins = bins;
        _uow = uow;
    }

    public async Task<ErrorOr<Success>> Handle(DeleteBinCommand cmd, CancellationToken ct)
    {
        var bin = await _bins.GetForUpdateAsync(cmd.Id, ct);
        if (bin is null)
            return AppErrors.Common.NotFound($"БИН с Id '{cmd.Id}' не найден.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _bins.Remove(bin);
            return Task.FromResult(true);
        }, ct);

        return Result.Success;
    }
}
