using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Domain.Common;
using MyCompany.Transfers.Domain.Helpers;
using MyCompany.Transfers.Domain.Providers;
using MyCompany.Transfers.Domain.Rates;
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

    //public decimal? ExchangeRate { get; private set; }   // 1 src = rate dst
    //public DateTimeOffset? RateTimestampUtc { get; private set; }

    public string? ProviderTransferId { get; private set; }  // ID перевода в системе провайдера
    public string? ProviderCode { get; private set; }        // сырой код провайдера
    public string? ErrorDescription { get; private set; }    // финальное описание для клиента/логов


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
        foreach (var kv in parameters) t._parameters[kv.Key] = kv.Value;
        t.CurrentQuote = quote;
        return t;
    }

    public void SetReceivedParams(IReadOnlyDictionary<string, string> receivedParamas)
    {
        foreach (var kv in receivedParamas) _provReceivedParams[kv.Key] = kv.Value;
    }

    public void MarkPrepared(Quote quote)
    {
        if (quote.IsExpired(DateTime.Now)) throw new DomainException("Quote expired");
        CurrentQuote = quote;
        Status = TransferStatus.PREPARED;
        PreparedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkConfirmed(DateTimeOffset now)
    {
        if (Status == TransferStatus.CONFIRMED) return;
        if (Status != TransferStatus.PREPARED) throw new DomainException("Only prepared can be confirmed.");
        if (CurrentQuote is null || CurrentQuote.IsExpired(now)) throw new DomainException("Quote invalid or expired.");

        Status = TransferStatus.CONFIRMED;
        ConfirmedAtUtc = now;
    }

    public void MarkCompleted(DateTimeOffset now, TransferStatus status)
    {
        if (Status == TransferStatus.SUCCESS || Status == TransferStatus.FAILED) return;
        if (Status != TransferStatus.CONFIRMED) throw new DomainException("Only confirmed can be completed.");
        
        Status = status;
        CompletedAtUtc = now;
    }

    public void MarkFailed(DateTimeOffset now)
    {
        if (Status == TransferStatus.PREPARED || Status == TransferStatus.CONFIRMED) return;

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

        if (kind == ProviderResultKind.Success)
        {
            CompletedAtUtc = now;
        }
        else if (kind == ProviderResultKind.Error || kind == ProviderResultKind.Technical)
        {
            CompletedAtUtc = now;
        }
        // Для Pending можно оставить CompletedAtUtc пустым, т.к. процесс ещё идёт
    }

    //DateTimeInfo ToDateTimeInfo(DateTimeOffset dateTime)
    //{
    //    var tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Dushanbe");

    //    var localDateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime.UtcDateTime, tz);
    //    var expiresAtLocal = new DateTimeOffset(localDateTime, tz.BaseUtcOffset).ToString("O");

    //    return new DateTimeInfo(

    //        dateTime.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
    //        DateTime.Parse(expiresAtLocal).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")
    //    );
    //}

    public CheckResponseDto ToCheckResponseDto(string currency, string dstCurrency, decimal rate) => new()
    {
        AvailableCurrencies = new List<CurrencyDto> {
            new CurrencyDto(currency, dstCurrency, Math.Round(rate, 4))
        },
        ResolvedParameters = new Dictionary<string, string>(_parameters)
    };

    public PrepareResponseDto ToPrepareResponseDto(Agent agent) => new()
    {
        TransferId = Id.ToString("N"),
        ExternalId = ExternalId,
        ServiceId = ServiceId,
        Source = new MoneyDto
        {
            Amount = Amount.Minor,
            Currency = Amount.Currency
        },
        Fee = new MoneyDto
        {
            Amount = CurrentQuote?.Fee.Minor ?? 0,
            Currency = CurrentQuote?.Fee.Currency ?? string.Empty
        },
        Total = new MoneyDto
        {
            Amount = CurrentQuote?.Total.Minor ?? Amount.Minor,
            Currency = CurrentQuote?.Fee.Currency ?? string.Empty
        },
        Credit = new MoneyDto
        {
            Amount = CurrentQuote!.CreditedAmount.Minor,
            Currency = CurrentQuote!.CreditedAmount.Currency,
        },
        QuotationId = CurrentQuote?.Id ?? "",
        ExpiresAt = new ResponseDateTimeInfo(CurrentQuote?.ExpiresAt ?? DateTimeOffset.UtcNow.AddMinutes(5), agent.TimeZoneId),
        ResolvedParameters = new Dictionary<string, string>(_parameters),
        Limits = new LimitInfoDto
        {
            Remaining = new MoneyDto
            {
                Amount = agent.Balances.First(x => x.Key == Amount.Currency).Value,
                Currency = Amount.Currency
            }
        },
        Rate = Math.Round(CurrentQuote!.ExchangeRate ?? 0, 4),
        Status = Status.ToResponse()
    };

    public ConfirmResponseDto ToConfirmResponseDto(Agent agent) => new()
    {
        TransferId = Id.ToString(),
        ExternalId = ExternalId,
        ServiceId = ServiceId,
        Source = new MoneyDto
        {
            Amount = Amount.Minor,
            Currency = Amount.Currency
        },
        Fee = new MoneyDto
        {
            Amount = CurrentQuote?.Fee.Minor ?? 0,
            Currency = CurrentQuote?.Fee.Currency ?? string.Empty
        },
        Total = new MoneyDto
        {
            Amount = CurrentQuote?.Total.Minor ?? Amount.Minor,
            Currency = CurrentQuote?.Fee.Currency ?? string.Empty
        },
        Credit = new MoneyDto
        {
            Amount = CurrentQuote!.CreditedAmount.Minor,
            Currency = CurrentQuote!.CreditedAmount.Currency,
        },
        Limits = new LimitInfoDto
        {
            Remaining = new MoneyDto
            {
                Amount = agent.Balances.First(x => x.Key == Amount.Currency).Value,
                Currency = Amount.Currency
            }
        },
        Status = Status.ToResponse(),
        ConfirmedAt = new ResponseDateTimeInfo(ConfirmedAtUtc ?? DateTimeOffset.UtcNow, agent.TimeZoneId),
    };

    public StatusResponseDto ToStatusResponseDto(Agent agent) => new()
    {
        TransferId = Id.ToString(),
        ExternalId = ExternalId,
        ServiceId = ServiceId,
        Source = new MoneyDto
        {
            Amount = Amount.Minor,
            Currency = Amount.Currency
        },
        Fee = new MoneyDto
        {
            Amount = CurrentQuote?.Fee.Minor ?? 0,
            Currency = CurrentQuote?.Fee.Currency ?? string.Empty
        },
        Total = new MoneyDto
        {
            Amount = CurrentQuote?.Total.Minor ?? Amount.Minor,
            Currency = CurrentQuote?.Fee.Currency ?? string.Empty
        },
        Credit = new MoneyDto
        {
            Amount = CurrentQuote!.CreditedAmount.Minor,
            Currency = CurrentQuote!.CreditedAmount.Currency,
        },
        Limits = new LimitInfoDto
        {
            Remaining = new MoneyDto
            {
                Amount = agent.Balances.First(x => x.Key == Amount.Currency).Value,
                Currency = Amount.Currency
            }
        },
        Status = Status.ToResponse(),
        ConfirmedAt = new ResponseDateTimeInfo(ConfirmedAtUtc ?? DateTimeOffset.UtcNow, agent.TimeZoneId),
    };
}