namespace MyCompany.Transfers.Domain.Transfers.Dtos;

public sealed record CurrencyDto(string BaseCurrency, string Currency, decimal Rate);