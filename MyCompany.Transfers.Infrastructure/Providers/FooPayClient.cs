using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Providers;
using System.Net.Http.Json;

namespace MyCompany.Transfers.Infrastructure.Providers;

//internal sealed class FooPayClient : IProviderClient
//{
//    public string ProviderId => "FooPay";
//    private readonly HttpClient _http;

//    public FooPayClient(HttpClient http) => _http = http;

//    public async Task<ProviderResult> SendAsync(ProviderRequest r, CancellationToken ct)
//    {
//        var payload = new
//        {
//            id = r.TransferId,
//            extId = r.ExternalId,
//            service = r.ServiceId,
//            account = r.Account,
//            amountMinor = r.AmountMinor,
//            currency = r.Currency,
//            parameters = r.Parameters
//        };

//        using var resp = await _http.PostAsJsonAsync("/api/payments", payload, ct);
//        if (!resp.IsSuccessStatusCode)
//            return new ProviderResult(false, null, $"HTTP {((int)resp.StatusCode)}");
//        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: ct);
//        body!.TryGetValue("operationId", out var opId);
//        return new ProviderResult(true, opId, null);
//    }

//    public Task<ProviderCheckResult> CheckAsync(ProviderRequest request, CancellationToken ct)
//    {
//        throw new NotImplementedException();
//    }
//}