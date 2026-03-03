using ErrorOr;

namespace MyCompany.Transfers.Application.Common.Behaviors;

/// <summary>
/// Исключение при ошибках валидации запроса (Commands/Queries).
/// Обрабатывается в API и преобразуется в 400 Bad Request с телом в формате ApiErrorResponse.
/// </summary>
public sealed class RequestValidationException : Exception
{
    public IReadOnlyList<Error> Errors { get; }

    public RequestValidationException(IReadOnlyList<Error> errors)
        : base($"Валидация не пройдена: {errors.Count} ошибок.")
    {
        Errors = errors;
    }
}
