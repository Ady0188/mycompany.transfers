using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Common;
using MyCompany.Transfers.Domain.Helpers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Transfers.Dtos;

namespace MyCompany.Transfers.Domain.Transfers;

public sealed class Transfer : IAggregateRoot
{
    public Guid Id { get; private set; }
    public long NumId { get; private set; }
    public string AgentId { get; private set; } = default!;
    public string TerminalId { get; private set; } = default!;
    public string ExternalId { get; private set; } = default!;
    public string ServiceId { get; private set; } = default!;
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

    public TransferStatus Status { get; private set; }
    public Quote? CurrentQuote { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? PreparedAtUtc { get; private set; }
    public DateTimeOffset? ConfirmedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    private Transfer() { }

    public static Transfer CreatePrepare(
        string agentId, string terminalId, string externalId, string serviceId,
        TransferMethod method, string account, long amountMinor, string currency,
        IDictionary<string, string> parameters, Quote quote)
    {
        var t = new Transfer
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            TerminalId = terminalId,
            ExternalId = externalId,
            ServiceId = serviceId,
            Method = method,
            Account = account,
            Amount = new Money(amountMinor, currency),
            Status = TransferStatus.PREPARED,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            PreparedAtUtc = DateTimeOffset.UtcNow
        };
        foreach (var kv in parameters)
            t._parameters[kv.Key] = kv.Value;
        t.CurrentQuote = quote;
        return t;
    }

    public void SetReceivedParams(IReadOnlyDictionary<string, string> receivedParams)
    {
        foreach (var kv in receivedParams)
            _provReceivedParams[kv.Key] = kv.Value;
    }

    public void MarkPrepared(Quote quote)
    {
        if (quote.IsExpired(DateTimeOffset.UtcNow))
            throw new DomainException("Quote expired");
        CurrentQuote = quote;
        Status = TransferStatus.PREPARED;
        PreparedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkConfirmed(DateTimeOffset now)
    {
        if (Status == TransferStatus.CONFIRMED) return;
        if (Status != TransferStatus.PREPARED)
            throw new DomainException("Only prepared can be confirmed.");
        if (CurrentQuote is null || CurrentQuote.IsExpired(now))
            throw new DomainException("Quote invalid or expired.");
        Status = TransferStatus.CONFIRMED;
        ConfirmedAtUtc = now;
    }

    public void MarkCompleted(DateTimeOffset now, TransferStatus status)
    {
        if (Status == TransferStatus.SUCCESS || Status == TransferStatus.FAILED) return;
        if (Status != TransferStatus.CONFIRMED)
            throw new DomainException("Only confirmed can be completed.");
        Status = status;
        CompletedAtUtc = now;
    }

    public void MarkFailed(DateTimeOffset now)
    {
        if (Status == TransferStatus.SUCCESS || Status == TransferStatus.FAILED) return;
        Status = TransferStatus.FAILED;
        CompletedAtUtc = now;
    }

    public void ApplyProviderResult(
        DateTimeOffset now,
        string providerCode,
        string? description,
        string? providerTransferId,
        ProviderResultKind kind,
        TransferStatus status)
    {
        ProviderCode = providerCode;
        ErrorDescription = description;
        ProviderTransferId = providerTransferId;
        Status = status;
        if (kind == ProviderResultKind.Success || kind == ProviderResultKind.Error || kind == ProviderResultKind.Technical)
            CompletedAtUtc = now;
    }

    public CheckResponseDto ToCheckResponseDto(string currency, string dstCurrency, decimal rate) => new()
    {
        AvailableCurrencies = new List<CurrencyDto> { new CurrencyDto(currency, dstCurrency, Math.Round(rate, 4)) },
        ResolvedParameters = new Dictionary<string, string>(_parameters)
    };

    public PrepareResponseDto ToPrepareResponseDto(Agent agent)
    {
        var remainingAmount = agent.Balances.TryGetValue(Amount.Currency, out var balance) ? balance : 0L;
        return new PrepareResponseDto
        {
            TransferId = Id.ToString("N"),
            ExternalId = ExternalId,
            ServiceId = ServiceId,
            Source = new MoneyDto { Amount = Amount.Minor, Currency = Amount.Currency },
            Fee = new MoneyDto { Amount = CurrentQuote?.Fee.Minor ?? 0, Currency = CurrentQuote?.Fee.Currency ?? string.Empty },
            Total = new MoneyDto { Amount = CurrentQuote?.Total.Minor ?? Amount.Minor, Currency = CurrentQuote?.Fee.Currency ?? string.Empty },
            Credit = new MoneyDto { Amount = CurrentQuote!.CreditedAmount.Minor, Currency = CurrentQuote!.CreditedAmount.Currency },
            QuotationId = CurrentQuote?.Id ?? "",
            ExpiresAt = new ResponseDateTimeInfo(CurrentQuote?.ExpiresAt ?? DateTimeOffset.UtcNow.AddMinutes(5), agent.TimeZoneId),
            ResolvedParameters = new Dictionary<string, string>(_parameters),
            Limits = new LimitInfoDto { Remaining = new MoneyDto { Amount = remainingAmount, Currency = Amount.Currency } },
            Rate = Math.Round(CurrentQuote!.ExchangeRate ?? 0, 4),
            Status = Status.ToResponse()
        };
    }

    public ConfirmResponseDto ToConfirmResponseDto(Agent agent)
    {
        var remainingAmount = agent.Balances.TryGetValue(Amount.Currency, out var balance) ? balance : 0L;
        return new ConfirmResponseDto
        {
            TransferId = Id.ToString(),
            ExternalId = ExternalId,
            ServiceId = ServiceId,
            Source = new MoneyDto { Amount = Amount.Minor, Currency = Amount.Currency },
            Fee = new MoneyDto { Amount = CurrentQuote?.Fee.Minor ?? 0, Currency = CurrentQuote?.Fee.Currency ?? string.Empty },
            Total = new MoneyDto { Amount = CurrentQuote?.Total.Minor ?? Amount.Minor, Currency = CurrentQuote?.Fee.Currency ?? string.Empty },
            Credit = new MoneyDto { Amount = CurrentQuote!.CreditedAmount.Minor, Currency = CurrentQuote!.CreditedAmount.Currency },
            Limits = new LimitInfoDto { Remaining = new MoneyDto { Amount = remainingAmount, Currency = Amount.Currency } },
            Status = Status.ToResponse(),
            ConfirmedAt = new ResponseDateTimeInfo(ConfirmedAtUtc ?? DateTimeOffset.UtcNow, agent.TimeZoneId)
        };
    }

    public StatusResponseDto ToStatusResponseDto(Agent agent)
    {
        var remainingAmount = agent.Balances.TryGetValue(Amount.Currency, out var balance) ? balance : 0L;
        return new StatusResponseDto
        {
            TransferId = Id.ToString(),
            ExternalId = ExternalId,
            ServiceId = ServiceId,
            Source = new MoneyDto { Amount = Amount.Minor, Currency = Amount.Currency },
            Fee = new MoneyDto { Amount = CurrentQuote?.Fee.Minor ?? 0, Currency = CurrentQuote?.Fee.Currency ?? string.Empty },
            Total = new MoneyDto { Amount = CurrentQuote?.Total.Minor ?? Amount.Minor, Currency = CurrentQuote?.Fee.Currency ?? string.Empty },
            Credit = new MoneyDto { Amount = CurrentQuote!.CreditedAmount.Minor, Currency = CurrentQuote!.CreditedAmount.Currency },
            Limits = new LimitInfoDto { Remaining = new MoneyDto { Amount = remainingAmount, Currency = Amount.Currency } },
            Status = Status.ToResponse(),
            ConfirmedAt = new ResponseDateTimeInfo(ConfirmedAtUtc ?? DateTimeOffset.UtcNow, agent.TimeZoneId)
        };
    }
}
