using MyCompany.Transfers.Domain.Agents;

namespace MyCompany.Transfers.Application.Common.Interfaces;

/// <summary>Создание зашифрованного ZIP-архива с данными терминала (AES). Пароль генерируется и возвращается один раз.</summary>
public interface ITerminalCredentialsArchiveBuilder
{
    /// <summary>Собрать ZIP с JSON данных терминала, зашифровать паролем AES-256. Возвращает поток архива и сгенерированный пароль (передать партнёру другим каналом).</summary>
    (Stream ZipStream, string GeneratedPassword) Build(Terminal terminal);
}
