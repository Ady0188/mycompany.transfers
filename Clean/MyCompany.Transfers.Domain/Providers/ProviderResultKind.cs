namespace MyCompany.Transfers.Domain.Providers;

public enum ProviderResultKind
{
    Success = 0,
    Pending = 1,
    Error = 2,
    Technical = 3,
    NoResponse = 4,
    Setting = 5
}
