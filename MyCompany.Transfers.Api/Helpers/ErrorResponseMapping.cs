using ErrorOr;
using MyCompany.Transfers.Contract.Core.Responses;

namespace MyCompany.Transfers.Api.Helpers;

public static class ErrorResponseMapping
{
    public static ApiErrorResponse ToApiErrorResponse(this Error error, HttpContext http)
    {
        int? numericCode = null;
        if (error.Metadata is not null && error.Metadata.TryGetValue("numericCode", out var numericCodeObj))
            numericCode = numericCodeObj as int?;

        return new ApiErrorResponse
        {
            Code = error.Code,
            NumericCode = numericCode,
            Message = error.Description,
            TraceId = http.TraceIdentifier
        };
    }
}
