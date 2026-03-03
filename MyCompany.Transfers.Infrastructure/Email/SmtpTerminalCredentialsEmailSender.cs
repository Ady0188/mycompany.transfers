using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyCompany.Transfers.Application.Common.Interfaces;

namespace MyCompany.Transfers.Infrastructure.Email;

public sealed class SmtpTerminalCredentialsEmailSender : ITerminalCredentialsEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpTerminalCredentialsEmailSender> _logger;

    public SmtpTerminalCredentialsEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpTerminalCredentialsEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, Stream attachmentContent, string attachmentFileName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogWarning("Smtp:Host не задан. Отправка письма пропущена.");
            return;
        }

        using var mail = new MailMessage();
        mail.From = new MailAddress(_options.FromAddress, _options.FromDisplayName);
        mail.To.Add(toEmail);
        mail.Subject = subject;
        mail.Body = htmlBody;
        mail.IsBodyHtml = true;

        attachmentContent.Position = 0;
        var attachment = new Attachment(attachmentContent, attachmentFileName, "application/zip");
        mail.Attachments.Add(attachment);

        using var smtp = new SmtpClient(_options.Host, _options.Port);
        smtp.EnableSsl = _options.EnableSsl;
        if (!string.IsNullOrEmpty(_options.UserName))
            smtp.Credentials = new NetworkCredential(_options.UserName, _options.Password);

        await smtp.SendMailAsync(mail, ct);
        _logger.LogInformation("Письмо с данными терминала отправлено на {To}", toEmail);
    }
}
