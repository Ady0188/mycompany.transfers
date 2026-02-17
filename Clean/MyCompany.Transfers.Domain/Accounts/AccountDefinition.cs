using MyCompany.Transfers.Domain.Accounts.Enums;
using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Accounts;

public sealed class AccountDefinition
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = default!;
    public string? Regex { get; private set; }
    public AccountNormalizeMode Normalize { get; private set; }
    public AccountAlgorithm Algorithm { get; private set; }
    public int? MinLength { get; private set; }
    public int? MaxLength { get; private set; }

    private AccountDefinition() { }

    private AccountDefinition(Guid id, string code, string? regex, AccountNormalizeMode normalize, AccountAlgorithm algorithm, int? minLength, int? maxLength)
    {
        Id = id;
        Code = code;
        Regex = regex;
        Normalize = normalize;
        Algorithm = algorithm;
        MinLength = minLength;
        MaxLength = maxLength;
    }

    /// <summary>
    /// Фабрика создания определения счёта (DDD). Id генерируется автоматически.
    /// </summary>
    public static AccountDefinition Create(string code, string? regex = null, AccountNormalizeMode normalize = AccountNormalizeMode.None, AccountAlgorithm algorithm = AccountAlgorithm.None, int? minLength = null, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Code определения счёта обязателен.");
        return new AccountDefinition(Guid.NewGuid(), code, regex, normalize, algorithm, minLength, maxLength);
    }

    /// <summary>
    /// Создание с заданным Id (для тестов или миграций).
    /// </summary>
    public static AccountDefinition CreateWithId(Guid id, string code, string? regex = null, AccountNormalizeMode normalize = AccountNormalizeMode.None, AccountAlgorithm algorithm = AccountAlgorithm.None, int? minLength = null, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Code определения счёта обязателен.");
        return new AccountDefinition(id, code, regex, normalize, algorithm, minLength, maxLength);
    }

    /// <summary>
    /// Обновление профиля определения счёта.
    /// </summary>
    public void UpdateProfile(string? code = null, string? regex = null, AccountNormalizeMode? normalize = null, AccountAlgorithm? algorithm = null, int? minLength = null, int? maxLength = null)
    {
        if (!string.IsNullOrWhiteSpace(code)) Code = code;
        if (regex is not null) Regex = regex;
        if (normalize.HasValue) Normalize = normalize.Value;
        if (algorithm.HasValue) Algorithm = algorithm.Value;
        if (minLength is not null) MinLength = minLength;
        if (maxLength is not null) MaxLength = maxLength;
    }
}
