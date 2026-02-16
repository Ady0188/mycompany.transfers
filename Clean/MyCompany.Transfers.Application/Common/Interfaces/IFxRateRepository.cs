namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IFxRateRepository
{
    /// <summary>Returns agent-specific FX rate. Different agents may have different rates.</summary>
    Task<(decimal rate, DateTimeOffset asOfUtc)?> GetAsync(string agentId, string baseCcy, string quoteCcy, CancellationToken ct);
}
