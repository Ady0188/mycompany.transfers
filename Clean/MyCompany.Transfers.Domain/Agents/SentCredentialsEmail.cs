using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Agents;

/// <summary>Запись об отправленном письме с данными терминала агенту.</summary>
public sealed class SentCredentialsEmail : IEntity
{
    public Guid Id { get; private set; }
    public string AgentId { get; private set; } = default!;
    public string TerminalId { get; private set; } = default!;
    public string ToEmail { get; private set; } = default!;
    public string Subject { get; private set; } = default!;
    public DateTime SentAtUtc { get; private set; }

    private SentCredentialsEmail() { }

    internal SentCredentialsEmail(Guid id, string agentId, string terminalId, string toEmail, string subject, DateTime sentAtUtc)
    {
        Id = id;
        AgentId = agentId;
        TerminalId = terminalId;
        ToEmail = toEmail;
        Subject = subject;
        SentAtUtc = sentAtUtc;
    }

    public static SentCredentialsEmail Record(string agentId, string terminalId, string toEmail, string subject)
    {
        return new SentCredentialsEmail(
            Guid.NewGuid(),
            agentId,
            terminalId,
            toEmail,
            subject ?? "",
            DateTime.UtcNow);
    }
}
