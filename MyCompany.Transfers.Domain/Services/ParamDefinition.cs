using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Services;

public sealed class ParamDefinition
{
    public string Id { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string? Name { get; private set; }
    public string? Description { get; private set; }
    public string? Regex { get; private set; }
    public bool Active { get; private set; } = true;

    private ParamDefinition() { }

    public ParamDefinition(string id, string code, string? name, string? description, string? regex, bool active = true)
    {
        Id = id;
        Code = code;
        Name = name;
        Description = description;
        Regex = regex;
        Active = active;
    }

    /// <summary>
    /// Фабрика создания параметра (DDD).
    /// </summary>
    public static ParamDefinition Create(string id, string code, string? name = null, string? description = null, string? regex = null, bool active = true)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("Id параметра обязателен.");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Code параметра обязателен.");
        return new ParamDefinition(id, code, name, description, regex, active);
    }

    /// <summary>
    /// Обновление профиля параметра.
    /// </summary>
    public void UpdateProfile(string? code = null, string? name = null, string? description = null, string? regex = null, bool? active = null)
    {
        if (!string.IsNullOrWhiteSpace(code)) Code = code;
        if (name is not null) Name = name;
        if (description is not null) Description = description;
        if (regex is not null) Regex = regex;
        if (active.HasValue) Active = active.Value;
    }
}
