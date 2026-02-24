namespace MyCompany.Transfers.Infrastructure.Email;

/// <summary>Настройки SMTP для отправки писем (учётные данные терминалов и т.д.).</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    /// <summary>Хост SMTP-сервера (например mail.example.com).</summary>
    public string Host { get; set; } = "";

    /// <summary>Порт (обычно 25, 587 для TLS).</summary>
    public int Port { get; set; } = 25;

    /// <summary>Использовать SSL/TLS.</summary>
    public bool EnableSsl { get; set; }

    /// <summary>Адрес отправителя (например ps@example.com).</summary>
    public string FromAddress { get; set; } = "";

    /// <summary>Отображаемое имя отправителя (например «Платежная система»).</summary>
    public string FromDisplayName { get; set; } = "";

    /// <summary>Логин для SMTP (если требуется).</summary>
    public string? UserName { get; set; }

    /// <summary>Пароль для SMTP (если требуется).</summary>
    public string? Password { get; set; }
}
