using MyCompany.Transfers.Domain.Accounts.Enums;

namespace MyCompany.Transfers.Domain.Accounts;

public sealed class AccountDefinition
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string? Regex { get; init; }
    public AccountNormalizeMode Normalize { get; init; }
    public AccountAlgorithm Algorithm { get; init; }
    public int? MinLength { get; init; }
    public int? MaxLength { get; init; }
}
