namespace MyCompany.Transfers.Admin.Client.Models;

/// <summary>Параметры фильтрации списка переводов.</summary>
public sealed class TransfersFilter
{
    public string? Id { get; set; }
    public string? AgentId { get; set; }
    public string? ExternalId { get; set; }
    public string? ProviderId { get; set; }
    public string? ServiceId { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }
    public string? Account { get; set; }

    public bool HasAnyValue =>
        !string.IsNullOrWhiteSpace(Id) ||
        !string.IsNullOrWhiteSpace(AgentId) ||
        !string.IsNullOrWhiteSpace(ExternalId) ||
        !string.IsNullOrWhiteSpace(ProviderId) ||
        !string.IsNullOrWhiteSpace(ServiceId) ||
        !string.IsNullOrWhiteSpace(Status) ||
        CreatedFrom.HasValue ||
        CreatedTo.HasValue ||
        !string.IsNullOrWhiteSpace(Account);

    public void Clear()
    {
        Id = null;
        AgentId = null;
        ExternalId = null;
        ProviderId = null;
        ServiceId = null;
        Status = null;
        CreatedFrom = null;
        CreatedTo = null;
        Account = null;
    }
}
