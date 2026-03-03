namespace MyCompany.Transfers.Domain.Transfers.Dtos;

public sealed record CheckResponseDto
{
    public List<CurrencyDto> AvailableCurrencies { get; set; } = new();
    public IReadOnlyDictionary<string, string> ResolvedParameters { get; init; } = new Dictionary<string, string>();
}

public sealed record CurrencyDto(string BaseCurrency, string Currency, decimal Rate);
