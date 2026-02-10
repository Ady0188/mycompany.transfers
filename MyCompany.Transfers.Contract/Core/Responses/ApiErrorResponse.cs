namespace MyCompany.Transfers.Contract.Core.Responses;

public sealed class ApiErrorResponse
{
    public string Code { get; init; } = default!;
    public int? NumericCode { get; init; }
    public string Message { get; init; } = default!;
    //public IDictionary<string, string[]>? Details { get; init; }
    public string? TraceId { get; init; }
}