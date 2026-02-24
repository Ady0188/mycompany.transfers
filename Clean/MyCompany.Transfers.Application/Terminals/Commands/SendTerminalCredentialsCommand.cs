using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Terminals.Commands;

public sealed record SendTerminalCredentialsCommand(
    string TerminalId,
    string ToEmail,
    string Body,
    string? Subject = null) : IRequest<ErrorOr<SendCredentialsResult>>;

public sealed class SendTerminalCredentialsCommandHandler : IRequestHandler<SendTerminalCredentialsCommand, ErrorOr<SendCredentialsResult>>
{
    private readonly ITerminalRepository _terminals;
    private readonly ITerminalCredentialsEmailSender _emailSender;
    private readonly ITerminalCredentialsArchiveBuilder _archiveBuilder;
    private readonly ISentCredentialsEmailRepository _sentHistory;
    private readonly IUnitOfWork _uow;

    public SendTerminalCredentialsCommandHandler(
        ITerminalRepository terminals,
        ITerminalCredentialsEmailSender emailSender,
        ITerminalCredentialsArchiveBuilder archiveBuilder,
        ISentCredentialsEmailRepository sentHistory,
        IUnitOfWork uow)
    {
        _terminals = terminals;
        _emailSender = emailSender;
        _archiveBuilder = archiveBuilder;
        _sentHistory = sentHistory;
        _uow = uow;
    }

    public async Task<ErrorOr<SendCredentialsResult>> Handle(SendTerminalCredentialsCommand request, CancellationToken ct)
    {
        var terminal = await _terminals.GetAsync(request.TerminalId, ct);
        if (terminal is null)
            return AppErrors.Common.NotFound($"Терминал '{request.TerminalId}' не найден.");

        if (string.IsNullOrWhiteSpace(request.ToEmail))
            return Error.Validation("SendCredentials.ToEmail", "Укажите адрес получателя.");

        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? $"Данные терминала {terminal.Name} ({terminal.Id})"
            : request.Subject;

        var (zipStream, password) = _archiveBuilder.Build(terminal);
        var fileName = $"terminal_{terminal.Id}_credentials.zip";
        try
        {
            await _emailSender.SendAsync(
                request.ToEmail.Trim(),
                subject,
                request.Body ?? "",
                zipStream,
                fileName,
                ct);
        }
        finally
        {
            await zipStream.DisposeAsync();
        }

        var record = SentCredentialsEmail.Record(terminal.AgentId, terminal.Id, request.ToEmail.Trim(), subject);
        _sentHistory.Add(record);
        await _uow.CommitChangesAsync(ct);

        return new SendCredentialsResult(password);
    }
}
