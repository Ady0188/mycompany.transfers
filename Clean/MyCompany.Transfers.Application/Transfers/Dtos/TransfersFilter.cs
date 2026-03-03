namespace MyCompany.Transfers.Application.Transfers.Dtos;

/// <summary>Параметры фильтрации списка переводов для админ-панели.</summary>
public sealed record TransfersFilter(
    Guid? Id = null,
    string? AgentId = null,
    string? ExternalId = null,
    string? ProviderId = null,
    string? ServiceId = null,
    string? Status = null,
    DateTimeOffset? CreatedFrom = null,
    DateTimeOffset? CreatedTo = null,
    string? Account = null);
