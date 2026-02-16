using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Application.Common.Providers;

public sealed record ProviderResult(OutboxStatus Status, Dictionary<string, string> ResponseFields, string? Error);
