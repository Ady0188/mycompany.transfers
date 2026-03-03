namespace MyCompany.Transfers.Domain.Transfers.Dtos;

public sealed class BalanceResponseDto
{
    public string AgentId { get; init; } = default!;
    public IReadOnlyList<MoneyDto> Balances { get; init; } = Array.Empty<MoneyDto>();
}
