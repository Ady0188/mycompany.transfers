using ErrorOr;

namespace MyCompany.Transfers.Application.Common.Helpers;

public static class AppErrors
{
    public static class Common
    {
        public static Error Unexpected(string? details = null) =>
            Error.Unexpected(
                code: "common.unexpected",
                description: details ?? "Произошла непредвиденная ошибка.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.CommonUnexpected); // 500

        public static Error Validation(string message, string code = "common.validation") =>
            Error.Validation(
                code: code,
                description: message, metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.CommonValidation); // 400

        public static Error Unauthorized(string? details = null) =>
            Error.Unauthorized(
                code: "auth.unauthorized",
                description: details ?? "Требуется аутентификация.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.AuthUnauthorized); // 401

        public static Error Forbidden(string? details = null) =>
            Error.Forbidden(
                code: "auth.forbidden",
                description: details ?? "Доступ запрещён.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.AuthForbidden); // 403

        public static Error NotFound(string? details = null) =>
            Error.NotFound(
                code: "common.not_found",
                description: details ?? "Не найден.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.CommonNotFound); // 404

        public static Error InvalidRequest(string message) =>
            Error.Validation(
                code: "common.invalid_request",
                description: message, metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.CommonInvalidRequest); // 400
    }

    public static class Transfers
    {
        public static Error NotFound(string externalId) =>
            Error.NotFound(
                code: "transfer.not_found",
                description: $"Перевод с ExternalId '{externalId}' не найден.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.TransferNotFound); // 404

        public static Error NotFoundById(Guid id) =>
            Error.NotFound(
                code: "transfer.not_found",
                description: $"Перевод с Id '{id}' не найден.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.TransferNotFound); // 404

        public static Error NotPrepared(string status) =>
            Error.Conflict(
                code: "transfer.not_prepared",
                description: $"Перевод не готов к подтверждению (статус: {status}).", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.TransferNotPrepared); // 409

        public static Error AlreadyFinished(string externalId, string status) =>
            Error.Conflict(
                code: "transfer.already_finished",
                description: $"Перевод '{externalId}' уже завершён (статус: {status}). Используйте метод получения статуса.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.TransferAlreadyFinished); // 409

        public static Error ExternalIdConflict(string externalId) =>
            Error.Conflict(
                code: "transfer.external_id_conflict",
                description: $"Перевод с ExternalId '{externalId}' уже существует с другими параметрами.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.TransferExternalIdConflict); // 409

        public static Error AlreadyConfirmed(string externalId) =>
            Error.Conflict(
                code: "transfer.already_confirmed",
                description: $"Перевод '{externalId}' уже подтверждён.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.TransferAlreadyConfirmed); // 409

        public static Error QuoteMismatch =>
            Error.Validation(
                code: "transfer.quote_mismatch",
                description: "Идентификатор котировки не совпадает.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.TransferQuoteMismatch); // 400

        public static Error QuoteExpired =>
            Error.Validation(
                code: "transfer.quote_expired",
                description: "Котировка истекла.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.TransferQuoteExpired); // 400

        public static Error InvalidRequest(string message) =>
            Error.Validation(
                code: "transfer.invalid_request",
                description: message, metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.TransferInvalidRequest); // 400
    }

    public static class Agents
    {
        public static Error NotFound(string agentId) =>
            Error.NotFound(
                code: "agent.not_found",
                description: $"Агент '{agentId}' не найден.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.AgentNotFound); // 404

        public static Error InsufficientBalance(string currency) =>
            Error.Conflict(
                code: "agent.insufficient_balance",
                description: $"Недостаточно средств в валюте {currency}.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.AgentInsufficientBalance); // 409
    }

    public static class Auth
    {
        public static Error SignatureInvalid =>
            Error.Unauthorized(
                code: "auth.bad_signature",
                description: "Некорректная подпись запроса.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.AuthBadSignature); // 401

        public static Error SignatureExpired =>
            Error.Unauthorized(
                code: "auth.signature_expired",
                description: "Срок действия подписи истёк.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.AuthBadSignature); // 401

        public static Error TerminalNotFound(string key) =>
            Error.Unauthorized(
                code: "auth.terminal_not_found",
                description: $"Терминал с ключом '{key}' не найден или отключён.", metadata: new Dictionary<string, object>())
            .WithMetadata("numericCode", ErrorCodes.TerminalNotFound); // 401
    }
}