using MyCompany.Transfers.Domain.Common;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Domain.Transfers;

public sealed class Outbox : IAggregateRoot
{
    public Guid TransferId { get; private set; }
    public long NumId { get; private set; }
    public string Source { get; private set; } = default!;
    public string AgentId { get; private set; } = default!;
    public string TerminalId { get; private set; } = default!;
    public string ExternalId { get; private set; } = default!;
    public string ServiceId { get; private set; } = default!;
    public string ProviderServicveId { get; private set; } = default!;
    public string ProviderId { get; private set; } = default!;
    public TransferMethod Method { get; private set; }
    public string Account { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;

    public string? ProviderTransferId { get; private set; }
    public string? ProviderCode { get; private set; }
    public string? ErrorDescription { get; private set; }

    public IReadOnlyDictionary<string, string> Parameters => _parameters;
    private readonly Dictionary<string, string> _parameters = new();

    public IReadOnlyDictionary<string, string> ProvReceivedParams => _provReceivedParams;
    private readonly Dictionary<string, string> _provReceivedParams = new();

    public OutboxStatus Status { get; private set; }
    public Quote? CurrentQuote { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? PreparedAtUtc { get; private set; }
    public DateTimeOffset? ConfirmedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private Outbox() { }

    public static Outbox Create(Transfer t, Service service, string source)
    {
        var o = new Outbox
        {
            TransferId = t.Id,
            NumId = t.NumId,
            AgentId = t.AgentId,
            TerminalId = t.TerminalId,
            ExternalId = t.ExternalId,
            ServiceId = t.ServiceId,
            Method = t.Method,
            Account = t.Account,
            Amount = t.Amount,
            Status = OutboxStatus.TO_SEND,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ConfirmedAtUtc = t.ConfirmedAtUtc,
            CurrentQuote = t.CurrentQuote,
            ErrorDescription = t.ErrorDescription,
            PreparedAtUtc = t.PreparedAtUtc,
            ProviderId = service.ProviderId,
            Source = source,
            ProviderServicveId = service.ProviderServicveId
        };

        foreach (var kv in t.Parameters) o._parameters[kv.Key] = kv.Value;
        
        return o;
    }
    
    public void SetReceivedParams(IReadOnlyDictionary<string, string> receivedParamas)
    {
        foreach (var kv in receivedParamas) _provReceivedParams[kv.Key] = kv.Value;
    }

    public void MarkCompleted(DateTimeOffset now, OutboxStatus status)
    {
        if (Status == OutboxStatus.SUCCESS || Status == OutboxStatus.FAILED) return;
        
        Status = status;
        CompletedAtUtc = now;
    }

    public void ApplyProviderResult(
        DateTimeOffset now,
        string providerCode,
        string? description,
        string? providerTransferId,
        ProviderResultKind kind,
        OutboxStatus status)
    {
        ProviderCode = providerCode;
        ErrorDescription = description;
        ProviderTransferId = providerTransferId;

        Status = status;

        if (kind == ProviderResultKind.Success)
        {
            CompletedAtUtc = now;
        }
        else if (kind == ProviderResultKind.Error || kind == ProviderResultKind.Technical)
        {
            CompletedAtUtc = now;
        }
    }
}