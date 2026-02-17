namespace MyCompany.Transfers.Infrastructure.Helpers;

/// <summary>
/// Валюта для шаблонов провайдеров (код alpha3/numeric, scale и т.д.).
/// Полная копия логики из оригинального проекта.
/// </summary>
internal sealed class Currency : IFormattable
{
    public string Alpha3 { get; }
    public string Numeric { get; }
    public int MinorUnit { get; }

    public Currency(string alpha3)
    {
        if (string.IsNullOrWhiteSpace(alpha3))
            throw new ArgumentNullException(nameof(alpha3));

        Alpha3 = alpha3.ToUpperInvariant();

        (Numeric, MinorUnit) = Alpha3 switch
        {
            // СНГ
            "TJS" => ("972", 2),
            "RUB" => ("643", 2),
            "KZT" => ("398", 2),
            "UZS" => ("860", 2),
            "AMD" => ("051", 2),
            "AZN" => ("944", 2),

            // Основные
            "USD" => ("840", 2),
            "EUR" => ("978", 2),
            "GBP" => ("826", 2),
            "CHF" => ("756", 2),
            "CNY" => ("156", 2),

            // Без копеек
            "JPY" => ("392", 0),
            "KRW" => ("410", 0),

            // 3 знака
            "KWD" => ("414", 3),
            "BHD" => ("048", 3),

            _ => throw new ArgumentException($"Unknown currency code: {alpha3}")
        };
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
        => format?.ToLowerInvariant() switch
        {
            null or "alpha3" => Alpha3,
            "numeric" => Numeric,
            "minor" or "scale" => MinorUnit.ToString(),
            _ => throw new FormatException($"Unsupported currency format: {format}")
        };

    public override string ToString() => Alpha3;
}

/// <summary>
/// Страна для шаблонов провайдеров (alpha2/alpha3/numeric).
/// Полная копия логики из оригинального проекта.
/// </summary>
internal sealed class Country : IFormattable
{
    public string Alpha2 { get; }
    public string Alpha3 { get; }
    public string Numeric { get; }

    public Country(string alpha2)
    {
        Alpha2 = (alpha2 ?? string.Empty).ToUpperInvariant();

        (Alpha3, Numeric) = Alpha2 switch
        {
            // СНГ
            "TJ" => ("TJK", "762"),
            "RU" => ("RUS", "643"),
            "UZ" => ("UZB", "860"),
            "KZ" => ("KAZ", "398"),
            "KG" => ("KGZ", "417"),
            "TM" => ("TKM", "795"),
            "AZ" => ("AZE", "031"),
            "AM" => ("ARM", "051"),
            "GE" => ("GEO", "268"),
            "UA" => ("UKR", "804"),
            "BY" => ("BLR", "112"),
            "MD" => ("MDA", "498"),

            // Европа
            "DE" => ("DEU", "276"),
            "FR" => ("FRA", "250"),
            "IT" => ("ITA", "380"),
            "ES" => ("ESP", "724"),
            "GB" => ("GBR", "826"),
            "NL" => ("NLD", "528"),
            "PL" => ("POL", "616"),
            "CZ" => ("CZE", "203"),
            "SK" => ("SVK", "703"),
            "RO" => ("ROU", "642"),
            "BG" => ("BGR", "100"),

            // Азия
            "CN" => ("CHN", "156"),
            "JP" => ("JPN", "392"),
            "KR" => ("KOR", "410"),
            "IN" => ("IND", "356"),
            "TR" => ("TUR", "792"),
            "AE" => ("ARE", "784"),
            "SA" => ("SAU", "682"),
            "IR" => ("IRN", "364"),
            "PK" => ("PAK", "586"),

            // Америка
            "US" => ("USA", "840"),
            "CA" => ("CAN", "124"),
            "MX" => ("MEX", "484"),
            "BR" => ("BRA", "076"),
            "AR" => ("ARG", "032"),

            // Другое
            "IL" => ("ISR", "376"),
            "EG" => ("EGY", "818"),
            "ZA" => ("ZAF", "710"),

            _ => throw new ArgumentException($"Unknown country code: {alpha2}")
        };
    }

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        format?.ToLowerInvariant() switch
        {
            null or "alpha2" => Alpha2,
            "alpha3" => Alpha3,
            "numeric" => Numeric,
            _ => throw new FormatException($"Unsupported country format: {format}")
        };

    public override string ToString() => Alpha2;
}

