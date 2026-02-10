using MyCompany.Transfers.Domain.Common;
using System.Text.RegularExpressions;

namespace MyCompany.Transfers.Domain.Services;

public sealed class Service
{
    public string Id { get; private set; } = default!;
    public string ProviderId { get; private set; } = default!;
    public string ProviderServicveId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string[] AllowedCurrencies { get; private set; } = Array.Empty<string>();
    public string? FxRounding { get; private set; }
    public long MinAmountMinor { get; private set; }
    public long MaxAmountMinor { get; private set; }
    public Guid AccountDefinitionId { get; init; }

    // теперь здесь только связи
    public List<ServiceParamDefinition> Parameters { get; private set; } = new();

    private Service() { }

    public Service(string id, string providerId, string name,
        string[] allowedCurrencies, long minAmountMinor, long maxAmountMinor, string? fxRounding,
        IEnumerable<ServiceParamDefinition> parameters)
    {
        Id = id;
        ProviderId = providerId;
        Name = name;
        AllowedCurrencies = allowedCurrencies;
        FxRounding = fxRounding;
        MinAmountMinor = minAmountMinor;
        MaxAmountMinor = maxAmountMinor;
        Parameters = parameters.ToList();
    }

    public void ValidateAmountAndCurrency(long amountMinor, string currency)
    {
        if (!AllowedCurrencies.Contains(currency, StringComparer.OrdinalIgnoreCase))
            throw new DomainException($"Currency {currency} is not allowed.");
        if (amountMinor < MinAmountMinor || amountMinor > MaxAmountMinor)
            throw new DomainException($"Amount out of range [{MinAmountMinor}; {MaxAmountMinor}].");
    }

    public void ValidateParameters(
        IDictionary<string, string> provided,
        IReadOnlyList<ParamDefinition> catalog)
    {
        var catalogByCode = catalog.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);
        var catalogById = catalog.ToDictionary(p => p.Id);

        foreach (var key in provided.Keys.ToList())
        {
            if (!catalogByCode.TryGetValue(key, out var pd))
            {
                throw new DomainException($"Unknown parameter '{key}'.");
            }

            var val = provided[key];
            if (!string.IsNullOrWhiteSpace(pd.Regex) &&
                !string.IsNullOrWhiteSpace(val) &&
                !Regex.IsMatch(val, pd.Regex, RegexOptions.ECMAScript))
            {
                throw new DomainException($"Parameter '{pd.Code}' is invalid.");
            }
        }

        foreach (var def in Parameters)
        {
            if (!catalogById.TryGetValue(def.ParameterId, out var pd))
                throw new DomainException($"Unknown parameter '{def.ParameterId}'.");

            var code = pd.Code;

            if (def.Required &&
                (!provided.TryGetValue(code, out var v) || string.IsNullOrWhiteSpace(v)))
            {
                throw new DomainException($"Parameter '{code}' is required.");
            }

            if (!provided.TryGetValue(code, out var value))
            {
                provided[code] = string.Empty;
            }
        }

        //var catalogDict = catalog.ToDictionary(p => p.Code, p => p);
        //foreach (var key in provided.Keys.ToList())
        //{
        //    if (!catalogByCode.TryGetValue(key, out var pd))
        //    {
        //        throw new DomainException($"Unknown parameter '{key}'.");
        //    }

        //    var val = provided[key];
        //    if (!string.IsNullOrWhiteSpace(pd.Regex) &&
        //        !string.IsNullOrWhiteSpace(val) &&
        //        !Regex.IsMatch(val, pd.Regex, RegexOptions.ECMAScript))
        //    {
        //        throw new DomainException($"Parameter '{pd.Code}' is invalid.");
        //    }
        //}

        //var paramIds = Parameters.Select(p => p.ParameterId);
        //var catalogById = catalog
        //    .Where(p => paramIds.Contains(p.Id))
        //    .ToDictionary(p => p.Id, p => p);

        //foreach (var def in Parameters)
        //{
        //    if (!catalogById.TryGetValue(def.ParameterId, out var pd))
        //        throw new DomainException($"Unknown parameter '{def.ParameterId}'.");

        //    var code = pd.Code;

        //    if (def.Required &&
        //        (!provided.TryGetValue(code, out var v) || string.IsNullOrWhiteSpace(v)))
        //    {
        //        throw new DomainException($"Parameter '{code}' is required.");
        //    }
        //}
    }
}