using MyCompany.Transfers.Domain.Common;

namespace MyCompany.Transfers.Domain.Agents;

public sealed class AgentBalanceHistory : IEntity
{
    public Guid Id { get; private set; }
    public string AgentId { get; private set; } = default!;
    /// <summary>Для операций АБС — идентификатор документа; для переводов — null.</summary>
    public long? DocId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string Currency { get; private set; } = default!;
    public long CurrentBalanceMinor { get; private set; }
    public long IncomeMinor { get; private set; }
    public long NewBalanceMinor { get; private set; }
    public BalanceHistoryReferenceType ReferenceType { get; private set; }
    /// <summary>Уникальный ключ операции: для АБС — DocId.ToString(), для перевода — "{TransferId}:Debit" или "{TransferId}:Refund".</summary>
    public string ReferenceId { get; private set; } = default!;

    private AgentBalanceHistory() { }

    private AgentBalanceHistory(
        Guid id,
        string agentId,
        long? docId,
        DateTime createdAtUtc,
        string currency,
        long currentBalanceMinor,
        long incomeMinor,
        long newBalanceMinor,
        BalanceHistoryReferenceType referenceType,
        string referenceId)
    {
        Id = id;
        AgentId = agentId;
        DocId = docId;
        CreatedAtUtc = createdAtUtc;
        Currency = currency;
        CurrentBalanceMinor = currentBalanceMinor;
        IncomeMinor = incomeMinor;
        NewBalanceMinor = newBalanceMinor;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
    }

    /// <summary>Создать запись по документу АБС (кредит/дебет).</summary>
    public static AgentBalanceHistory CreateForAbsDocument(
        string agentId,
        long docId,
        DateTime createdAtUtc,
        string currency,
        long currentBalanceMinor,
        long incomeMinor,
        long newBalanceMinor)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            throw new DomainException("AgentId is required for balance history.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required for balance history.");

        return new AgentBalanceHistory(
            Guid.NewGuid(),
            agentId,
            docId,
            createdAtUtc,
            currency,
            currentBalanceMinor,
            incomeMinor,
            newBalanceMinor,
            BalanceHistoryReferenceType.AbsDocument,
            docId.ToString());
    }

    /// <summary>Создать запись по переводу (списание при подтверждении или возврат при ошибке).</summary>
    /// <param name="referenceId">Уникальный ключ: например "{transferId}:Debit" или "{transferId}:Refund".</param>
    public static AgentBalanceHistory CreateForTransfer(
        string agentId,
        string referenceId,
        DateTime createdAtUtc,
        string currency,
        long currentBalanceMinor,
        long incomeMinor,
        long newBalanceMinor)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            throw new DomainException("AgentId is required for balance history.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required for balance history.");
        if (string.IsNullOrWhiteSpace(referenceId))
            throw new DomainException("ReferenceId is required for transfer balance history.");

        return new AgentBalanceHistory(
            Guid.NewGuid(),
            agentId,
            null,
            createdAtUtc,
            currency,
            currentBalanceMinor,
            incomeMinor,
            newBalanceMinor,
            BalanceHistoryReferenceType.Transfer,
            referenceId);
    }
}
