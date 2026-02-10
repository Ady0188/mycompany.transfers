using ErrorOr;

namespace MyCompany.Transfers.Application.Common.Helpers;

public static class ErrorExtensions
{
    public static Error WithMetadata(this Error error, string key, object value)
    {
        error.Metadata[key] = value;
        return error;
    }
}
