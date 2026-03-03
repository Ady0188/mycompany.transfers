using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Bins.Dtos;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Bins;

namespace MyCompany.Transfers.Application.Bins.Commands;

/// <summary>
/// С клиента передаются только Prefix, Code, Name. Len вычисляется на сервере из длины Prefix.
/// </summary>
public sealed record CreateBinCommand(string Prefix, string Code, string Name) : IRequest<ErrorOr<BinAdminDto>>;

public sealed class CreateBinCommandHandler : IRequestHandler<CreateBinCommand, ErrorOr<BinAdminDto>>
{
    private readonly IBinRepository _bins;
    private readonly IUnitOfWork _uow;

    public CreateBinCommandHandler(IBinRepository bins, IUnitOfWork uow)
    {
        _bins = bins;
        _uow = uow;
    }

    public async Task<ErrorOr<BinAdminDto>> Handle(CreateBinCommand cmd, CancellationToken ct)
    {
        var prefix = (cmd.Prefix ?? "").Trim();
        var code = (cmd.Code ?? "").Trim();
        var name = (cmd.Name ?? "").Trim();

        if (await _bins.ExistsByPrefixAsync(prefix, null, ct))
            return AppErrors.Common.Validation($"БИН с префиксом '{prefix}' уже существует.");
        if (await _bins.ExistsByCodeAsync(code, null, ct))
            return AppErrors.Common.Validation($"БИН с кодом '{code}' уже существует.");

        var bin = Bin.Create(Guid.NewGuid(), prefix, code, name);

        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _bins.Add(bin);
            return Task.FromResult(true);
        }, ct);

        return BinAdminDto.FromDomain(bin);
    }
}
