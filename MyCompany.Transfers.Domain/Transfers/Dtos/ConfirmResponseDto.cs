namespace MyCompany.Transfers.Domain.Transfers.Dtos;

public class ConfirmResponseDto : TransferBaseDto
{
    public ResponseDateTimeInfo? ConfirmedAt { get; init; }
    public ResponseDateTimeInfo? CompletedAt { get; init; }
    public IReadOnlyDictionary<string, string> ResolvedParameters { get; init; } = new Dictionary<string, string>();
}
