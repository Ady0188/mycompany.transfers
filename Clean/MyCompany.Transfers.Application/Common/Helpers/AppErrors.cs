using ErrorOr;

namespace MyCompany.Transfers.Application.Common.Helpers;

public static class AppErrors
{
    public static class Common
    {
        public static Error Unexpected(string? details = null) =>
            Error.Unexpected("common.unexpected", details ?? "Произошла непредвиденная ошибка.", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.CommonUnexpected);

        public static Error Validation(string message, string code = "common.validation") =>
            Error.Validation(code, message, new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.CommonValidation);

        public static Error Unauthorized(string? details = null) =>
            Error.Unauthorized("auth.unauthorized", details ?? "Требуется аутентификация.", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.AuthUnauthorized);

        public static Error Forbidden(string? details = null) =>
            Error.Forbidden("auth.forbidden", details ?? "Доступ запрещён.", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.AuthForbidden);

        public static Error NotFound(string? details = null) =>
            Error.NotFound("common.not_found", details ?? "Не найден.", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.CommonNotFound);

        public static Error InvalidRequest(string message) =>
            Error.Validation("common.invalid_request", message, new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.CommonInvalidRequest);
    }

    public static class Transfers
    {
        public static Error NotFound(string externalId) =>
            Error.NotFound("transfer.not_found", $"Перевод с ExternalId '{externalId}' не найден.", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.TransferNotFound);

        public static Error NotFoundById(Guid id) =>
            Error.NotFound("transfer.not_found", $"Перевод с Id '{id}' не найден.", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.TransferNotFound);

        public static Error NotPrepared(string status) =>
            Error.Conflict("transfer.not_prepared", $"Перевод не готов к подтверждению (статус: {status}).", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.TransferNotPrepared);

        public static Error AlreadyFinished(string externalId, string status) =>
            Error.Conflict("transfer.already_finished", $"Перевод '{externalId}' уже завершён (статус: {status}).", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.TransferAlreadyFinished);

        public static Error ExternalIdConflict(string externalId) =>
            Error.Conflict("transfer.external_id_conflict", $"Перевод с ExternalId '{externalId}' уже существует.", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.TransferExternalIdConflict);

        public static Error QuoteMismatch =>
            Error.Validation("transfer.quote_mismatch", "Идентификатор котировки не совпадает.", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.TransferQuoteMismatch);

        public static Error QuoteExpired =>
            Error.Validation("transfer.quote_expired", "Котировка истекла.", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.TransferQuoteExpired);

        public static Error InvalidRequest(string message) =>
            Error.Validation("transfer.invalid_request", message, new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.TransferInvalidRequest);
    }

    public static class Agents
    {
        public static Error NotFound(string agentId) =>
            Error.NotFound("agent.not_found", $"Агент '{agentId}' не найден.", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.AgentNotFound);

        public static Error InsufficientBalance(string currency) =>
            Error.Conflict("agent.insufficient_balance", $"Недостаточно средств в валюте {currency}.", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.AgentInsufficientBalance);
    }

    public static class Auth
    {
        public static Error SignatureInvalid =>
            Error.Unauthorized("auth.bad_signature", "Некорректная подпись запроса.", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.AuthBadSignature);

        public static Error TerminalNotFound(string key) =>
            Error.Unauthorized("auth.terminal_not_found", $"Терминал с ключом '{key}' не найден.", new Dictionary<string, object>())
                .WithMetadata("numericCode", ErrorCodes.TerminalNotFound);
    }
}
