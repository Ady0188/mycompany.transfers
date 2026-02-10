using ErrorOr;
using MyCompany.Transfers.Contract.Core.Responses;

namespace MyCompany.Transfers.Api.Helpers;

public static class ContractsMapping
{
    public static ApiErrorResponse ToApiErrorResponse(this Error error, HttpContext http)
    {
        error.Metadata.TryGetValue("numericCode", out var numericCodeObj);
        int? numericCode = numericCodeObj as int?;

        return new ApiErrorResponse
        {
            Code = error.Code,
            NumericCode = numericCode,
            Message = error.Description,
            //Details = error.Type == ErrorType.Validation
            //    ? BuildValidationDetails(error) // если используешь ErrorOr с ValidationErrors
            //    : null,
            TraceId = http.TraceIdentifier
        };
    }

    private static Dictionary<string, string[]> BuildValidationDetails(Error error)
    {
        // Если ты используешь ErrorOr.Validation(), то error.Metadata["Errors"]
        // может содержать список детальных ошибок. Если нет — адаптируй под себя.

        if (error.Metadata.TryGetValue("Errors", out var obj) &&
            obj is List<Error> validationErrors)
        {
            return validationErrors
                .GroupBy(e => e.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Description).ToArray()
                );
        }

        // fallback для обычной валидации:
        return new Dictionary<string, string[]>
        {
            [error.Code] = new[] { error.Description }
        };
    }
}