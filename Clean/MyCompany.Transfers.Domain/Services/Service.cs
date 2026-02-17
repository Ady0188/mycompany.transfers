using MyCompany.Transfers.Domain.Common;
using System.Text.RegularExpressions;

namespace MyCompany.Transfers.Domain.Services;

public sealed class Service
{
    public string Id { get; private set; } = default!;
    public string ProviderId { get; private set; } = default!;
    public string ProviderServiceId { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string[] AllowedCurrencies { get; private set; } = Array.Empty<string>();
    public string? FxRounding { get; private set; }
    public long MinAmountMinor { get; private set; }
    public long MaxAmountMinor { get; private set; }
    public Guid AccountDefinitionId { get; private set; }
    public List<ServiceParamDefinition> Parameters { get; private set; } = new();

    private Service() { }

    public Service(string id, string providerId, string providerServiceId, string name,
        string[] allowedCurrencies, long minAmountMinor, long maxAmountMinor, string? fxRounding,
        Guid accountDefinitionId,
        IEnumerable<ServiceParamDefinition> parameters)
    {
        Id = id;
        ProviderId = providerId;
        ProviderServiceId = providerServiceId;
        Name = name;
        AllowedCurrencies = allowedCurrencies;
        MinAmountMinor = minAmountMinor;
        MaxAmountMinor = maxAmountMinor;
        FxRounding = fxRounding;
        AccountDefinitionId = accountDefinitionId;
        Parameters = parameters.ToList();
    }

    /// <summary>
    /// Фабрика создания услуги (DDD). Проверяет инварианты.
    /// </summary>
    public static Service Create(string id, string providerId, string providerServiceId, string name,
        string[] allowedCurrencies, long minAmountMinor, long maxAmountMinor, string? fxRounding,
        Guid accountDefinitionId,
        IEnumerable<ServiceParamDefinition> parameters)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("Id услуги обязателен.");
        if (string.IsNullOrWhiteSpace(providerId))
            throw new DomainException("ProviderId обязателен.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name обязателен.");
        if (minAmountMinor < 0 || maxAmountMinor < minAmountMinor)
            throw new DomainException("Некорректный диапазон сумм.");
        return new Service(id, providerId, providerServiceId ?? id, name,
            allowedCurrencies ?? Array.Empty<string>(), minAmountMinor, maxAmountMinor, fxRounding,
            accountDefinitionId, parameters ?? Enumerable.Empty<ServiceParamDefinition>());
    }

    /// <summary>
    /// Обновление профиля услуги. Переданные непустые значения заменяют текущие. Parameters заменяется при передаче не null.
    /// </summary>
    public void UpdateProfile(string? providerId = null, string? providerServiceId = null, string? name = null,
        string[]? allowedCurrencies = null, long? minAmountMinor = null, long? maxAmountMinor = null,
        string? fxRounding = null, Guid? accountDefinitionId = null, IEnumerable<ServiceParamDefinition>? parameters = null)
    {
        if (!string.IsNullOrWhiteSpace(providerId)) ProviderId = providerId;
        if (!string.IsNullOrWhiteSpace(providerServiceId)) ProviderServiceId = providerServiceId;
        if (!string.IsNullOrWhiteSpace(name)) Name = name;
        if (allowedCurrencies is not null) AllowedCurrencies = allowedCurrencies;
        if (minAmountMinor.HasValue) MinAmountMinor = minAmountMinor.Value;
        if (maxAmountMinor.HasValue) MaxAmountMinor = maxAmountMinor.Value;
        if (fxRounding is not null) FxRounding = fxRounding;
        if (accountDefinitionId.HasValue) AccountDefinitionId = accountDefinitionId.Value;
        if (parameters is not null)
        {
            Parameters.Clear();
            Parameters.AddRange(parameters);
        }
    }

    public void ValidateAmountAndCurrency(long amountMinor, string currency)
    {
        if (!AllowedCurrencies.Contains(currency, StringComparer.OrdinalIgnoreCase))
            throw new DomainException($"Currency {currency} is not allowed.");
        if (amountMinor < MinAmountMinor || amountMinor > MaxAmountMinor)
            throw new DomainException($"Amount out of range [{MinAmountMinor}; {MaxAmountMinor}].");
    }

    public void ValidateParameters(IDictionary<string, string> provided, IReadOnlyList<ParamDefinition> catalog)
    {
        var catalogByCode = catalog.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);
        var catalogById = catalog.ToDictionary(p => p.Id);

        foreach (var key in provided.Keys.ToList())
        {
            if (!catalogByCode.TryGetValue(key, out var pd))
                throw new DomainException($"Unknown parameter '{key}'.");

            var val = provided[key];
            if (!string.IsNullOrWhiteSpace(pd.Regex) &&
                !string.IsNullOrWhiteSpace(val) &&
                !Regex.IsMatch(val, pd.Regex, RegexOptions.ECMAScript))
                throw new DomainException($"Parameter '{pd.Code}' is invalid.");
        }

        foreach (var def in Parameters)
        {
            if (!catalogById.TryGetValue(def.ParameterId, out var pd))
                throw new DomainException($"Unknown parameter '{def.ParameterId}'.");

            var code = pd.Code;

            if (def.Required && (!provided.TryGetValue(code, out var v) || string.IsNullOrWhiteSpace(v)))
                throw new DomainException($"Parameter '{code}' is required.");

            if (!provided.TryGetValue(code, out _))
                provided[code] = string.Empty;
        }
    }
}
