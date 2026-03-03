namespace MyCompany.Transfers.Application.Common.Interfaces;

public interface IProviderTokenService
{
    Task<string?> GetAccessTokenAsync(string providerId, CancellationToken ct);

    Task<string> RefreshOn401Async(
        string providerId,
        Func<CancellationToken, Task<(string accessToken, DateTimeOffset? expiresAtUtc)>> loginFunc,
        CancellationToken ct);
}
