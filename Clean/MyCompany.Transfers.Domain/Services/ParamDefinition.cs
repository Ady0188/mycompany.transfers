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
}
