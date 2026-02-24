namespace MyCompany.Transfers.Application.Agents.Dtos;

public sealed record SentCredentialsEmailItemDto(
    Guid Id,
    string TerminalId,
    string ToEmail,
    string Subject,
    DateTime SentAtUtc);
