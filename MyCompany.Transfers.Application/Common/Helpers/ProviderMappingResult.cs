using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Application.Common.Helpers;

public sealed record ProviderMappingResult(
    bool Found,
    string ProviderCode,
    string? NormalizedCode,
    string? Description,
    ProviderResultKind Kind,
    TransferStatus Status,
    int? ErrorCode);