using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Application.Common.Providers;

public sealed record ProviderCheckResult(bool IsSuccess, Dictionary<string, string> ResponseFields, string? Error);
//public sealed record ProviderResult(ProviderResultKind Status, Dictionary<string, string> ResponseFields, string? Error);
public sealed record ProviderResult(OutboxStatus Status, Dictionary<string, string> ResponseFields, string? Error);
