using MyCompany.Transfers.Domain.Transfers;

namespace MyCompany.Transfers.Domain.Providers;

public class ProviderErrorCode
{
    public long Id { get; set; }

    public string ProviderId { get; set; } = null!;

    // "сырой" код провайдера, приходящий снаружи ("0", "10", "15", "20", "ERR01" и т.п.)
    public string ProviderCode { get; set; } = null!;

    // человекочитаемое описание
    public string Description { get; set; } = null!;

    // вид результата (успех / ожидание / ошибка / тех.ошибка)
    public ProviderResultKind Kind { get; set; }

    // во что должен перейти наш перевод
    public TransferStatus Status { get; set; }

    // необязательный NumericCode нашего API (можно ссылаться на ErrorCodes)
    public int? ErrorCode { get; set; }
}