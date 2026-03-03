using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Bins;

public sealed class Bin
{
    public Guid Id { get; private set; }
    public string Prefix { get; private set; } = default!;
    public int Len { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;

    private Bin() { }

    public Bin(Guid id, string prefix, int len, string code, string name)
    {
        Id = id;
        Prefix = prefix ?? "";
        Len = len;
        Code = code ?? "";
        Name = name ?? "";
    }

    /// <summary>
    /// Фабрика создания записи БИН (DDD). Len вычисляется из длины Prefix, если не задан.
    /// </summary>
    public static Bin Create(Guid id, string prefix, string code, string name, int? len = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Code обязателен.");
        var prefixNorm = prefix?.Trim() ?? "";
        var lenVal = len ?? prefixNorm.Length;
        return new Bin(id, prefixNorm, lenVal, code.Trim(), (name ?? "").Trim());
    }

    /// <summary>
    /// Обновление профиля. Len пересчитывается из Prefix при изменении Prefix.
    /// </summary>
    public void UpdateProfile(string? prefix = null, string? code = null, string? name = null, int? len = null)
    {
        if (prefix is not null)
        {
            Prefix = prefix.Trim();
            Len = len ?? Prefix.Length;
        }
        if (!string.IsNullOrWhiteSpace(code)) Code = code.Trim();
        if (name is not null) Name = name.Trim();
        if (len.HasValue) Len = len.Value;
    }
}
