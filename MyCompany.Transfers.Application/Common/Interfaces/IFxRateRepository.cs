namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IFxRateRepository
{
    Task<(decimal rate, DateTimeOffset asOfUtc)?> GetAsync(string baseCcy, string quoteCcy, CancellationToken ct);
}