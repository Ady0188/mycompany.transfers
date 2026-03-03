using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Bins.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Bins;

namespace MyCompany.Transfers.Application.Bins.Commands;

/// <summary>
/// С клиента передаются только Prefix, Code, Name. Len пересчитывается из Prefix.
/// </summary>
public sealed record UpdateBinCommand(Guid Id, string Prefix, string Code, string Name) : IRequest<ErrorOr<BinAdminDto>>;

public sealed class UpdateBinCommandHandler : IRequestHandler<UpdateBinCommand, ErrorOr<BinAdminDto>>
{
    private readonly IBinRepository _bins;
    private readonly IUnitOfWork _uow;

    public UpdateBinCommandHandler(IBinRepository bins, IUnitOfWork uow)
    {
        _bins = bins;
        _uow = uow;
    }

    public async Task<ErrorOr<BinAdminDto>> Handle(UpdateBinCommand cmd, CancellationToken ct)
    {
        var bin = await _bins.GetForUpdateAsync(cmd.Id, ct);
        if (bin is null)
            return AppErrors.Common.NotFound($"БИН с Id '{cmd.Id}' не найден.");

        var prefix = (cmd.Prefix ?? "").Trim();
        var code = (cmd.Code ?? "").Trim();
        var name = (cmd.Name ?? "").Trim();

        if (await _bins.ExistsByPrefixAsync(prefix, cmd.Id, ct))
            return AppErrors.Common.Validation($"БИН с префиксом '{prefix}' уже существует.");
        if (await _bins.ExistsByCodeAsync(code, cmd.Id, ct))
            return AppErrors.Common.Validation($"БИН с кодом '{code}' уже существует.");

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            bin.UpdateProfile(prefix, code, name);
            _bins.Update(bin);
            return Task.FromResult(true);
        }, ct);

        return BinAdminDto.FromDomain(bin);
    }
}
