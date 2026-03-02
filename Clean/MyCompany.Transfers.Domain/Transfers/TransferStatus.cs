namespace MyCompany.Transfers.Domain.Transfers;

public enum TransferStatus
{
    NEW,
    PREPARED,
    CONFIRMED,
    SUCCESS,
    TECHNICAL,
    FAILED,
    EXPIRED,
    FRAUD
}

public enum OutboxStatus
{
    TO_SEND,
    SENDING,
    STATUS,
    SUCCESS,
    TECHNICAL,
    FAILED,
    EXPIRED,
    FRAUD,
    NORESPONSE,
    SETTING,
    NOT_FOUND,
    SENT_TO_ABS
}
