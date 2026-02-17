using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Parameters.Dtos;

public sealed record ParameterAdminDto(
    string Id,
    string Code,
    string? Name,
    string? Description,
    string? Regex,
    bool Active)
{
    public static ParameterAdminDto FromDomain(ParamDefinition p) =>
        new(p.Id, p.Code, p.Name, p.Description, p.Regex, p.Active);
}
