namespace MyCompany.Transfers.Application.Common.Helpers;

public static class ErrorCodes
{
    public const int CommonUnexpected = 1000;
    public const int CommonValidation = 1001;
    public const int CommonNotFound = 1002;
    public const int CommonInvalidRequest = 1003;
    public const int AuthUnauthorized = 1100;
    public const int AuthForbidden = 1101;
    public const int AuthBadSignature = 1102;
    public const int TransferNotFound = 2001;
    public const int TransferNotPrepared = 2002;
    public const int TransferExternalIdConflict = 2003;
    public const int TransferAlreadyFinished = 2004;
    public const int TransferAlreadyConfirmed = 2005;
    public const int TransferQuoteMismatch = 2006;
    public const int TransferQuoteExpired = 2007;
    public const int TransferInvalidRequest = 2008;
    public const int AgentNotFound = 3001;
    public const int AgentInsufficientBalance = 3002;
    public const int TerminalNotFound = 4000;
}