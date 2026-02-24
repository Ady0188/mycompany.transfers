namespace MyCompany.Transfers.Application.Common.Interfaces;

/// <summary>Отправка письма с учётными данными терминала (архив + текст от имени компании).</summary>
public interface ITerminalCredentialsEmailSender
{
    /// <summary>Отправить письмо на указанный адрес с темой, HTML-телом и вложением (архив).</summary>
    Task SendAsync(string toEmail, string subject, string htmlBody, Stream attachmentContent, string attachmentFileName, CancellationToken ct = default);
}
