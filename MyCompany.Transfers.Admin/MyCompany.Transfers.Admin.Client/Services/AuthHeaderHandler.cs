using System.Net.Http.Headers;

namespace MyCompany.Transfers.Admin.Client.Services;

/// <summary>
/// Добавляет заголовок Authorization: Bearer для запросов к API.
/// </summary>
public sealed class AuthHeaderHandler : DelegatingHandler
{
    private readonly IAuthService _auth;

    public AuthHeaderHandler(IAuthService auth)
    {
        _auth = auth;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _auth.GetTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
