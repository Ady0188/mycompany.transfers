using ErrorOr;

namespace MyCompany.Transfers.Application.Common.Helpers;

public static class ErrorExtensions
{
    public static Error WithMetadata(this Error error, string key, object value)
    {
        if (error.Metadata is not null)
            error.Metadata[key] = value;
        return error;
    }
}
