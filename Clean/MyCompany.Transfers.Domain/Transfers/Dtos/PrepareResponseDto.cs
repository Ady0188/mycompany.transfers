namespace MyCompany.Transfers.Domain.Transfers.Dtos;

public sealed class PrepareResponseDto : TransferBaseDto
{
    public string QuotationId { get; init; } = default!;
    public decimal? Rate { get; init; }
    public ResponseDateTimeInfo ExpiresAt { get; init; } = default!;
    public IReadOnlyDictionary<string, string> ResolvedParameters { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<ValidationItem> Warnings { get; init; } = Array.Empty<ValidationItem>();
}

public sealed class MoneyDto
{
    public string Currency { get; init; } = default!;
    public long Amount { get; init; }
}

public sealed class LimitInfoDto
{
    public MoneyDto? Used { get; init; }
    public MoneyDto? Remaining { get; init; }
}

public sealed class ResponseDateTimeInfo
{
    public ResponseDateTimeInfo(DateTimeOffset utcDateTime, string timeZoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localDateTime = TimeZoneInfo.ConvertTime(utcDateTime, tz);
        Utc = utcDateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        Local = localDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
    }

    public string Utc { get; private init; } = default!;
    public string Local { get; private init; } = default!;
}

public sealed record ValidationItem(string Code, string Message);
