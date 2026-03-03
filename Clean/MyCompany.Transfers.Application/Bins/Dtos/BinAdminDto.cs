using MyCompany.Transfers.Domain.Bins;

namespace MyCompany.Transfers.Application.Bins.Dtos;

public sealed record BinAdminDto(
    Guid Id,
    string Prefix,
    int Len,
    string Code,
    string Name)
{
    public static BinAdminDto FromDomain(Bin b) =>
        new(b.Id, b.Prefix, b.Len, b.Code, b.Name);
}
