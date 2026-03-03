using MyCompany.Transfers.Domain.Accounts;
using MyCompany.Transfers.Domain.Accounts.Enums;

namespace MyCompany.Transfers.Application.AccountDefinitions.Dtos;

public sealed record AccountDefinitionAdminDto(
    Guid Id,
    string Code,
    string? Regex,
    AccountNormalizeMode Normalize,
    AccountAlgorithm Algorithm,
    int? MinLength,
    int? MaxLength)
{
    public static AccountDefinitionAdminDto FromDomain(AccountDefinition a) =>
        new(a.Id, a.Code, a.Regex, a.Normalize, a.Algorithm, a.MinLength, a.MaxLength);
}
