namespace MyCompany.Transfers.Domain.Transfers.Dtos;

public class TransferBaseDto
{
    public string TransferId { get; init; } = default!;
    public string? ExternalId { get; init; }
    public string ServiceId { get; init; } = default!;

    public MoneyDto Source { get; init; } = default!;
    public MoneyDto Fee { get; init; } = default!;
    public MoneyDto Total { get; init; } = default!;
    public MoneyDto Credit { get; init; } = default!;

    public LimitInfoDto? Limits { get; init; }
    public string Status { get; init; } = default!; // PREPARED | CONFIRMED | SUCCESS | FAILED | EXPIRED | FRAUD
    public string? StatusMessage { get; init; }
}
