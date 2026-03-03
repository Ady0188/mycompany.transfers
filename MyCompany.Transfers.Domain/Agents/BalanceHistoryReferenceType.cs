namespace MyCompany.Transfers.Domain.Agents;

/// <summary>
/// Тип операции в истории баланса: документ АБС или перевод (дебет/возврат).
/// </summary>
public enum BalanceHistoryReferenceType
{
    /// <summary>Кредит/дебет по документу АБС (DocId).</summary>
    AbsDocument = 0,

    /// <summary>Списание или возврат по переводу (ReferenceId = "{TransferId}:Debit" или "{TransferId}:Refund").</summary>
    Transfer = 1
}
