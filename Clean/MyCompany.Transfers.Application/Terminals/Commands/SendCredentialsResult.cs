namespace MyCompany.Transfers.Application.Terminals.Commands;

/// <summary>Результат отправки учётных данных: пароль для архива (показать один раз, передать партнёру другим каналом).</summary>
public sealed record SendCredentialsResult(string ArchivePassword);
