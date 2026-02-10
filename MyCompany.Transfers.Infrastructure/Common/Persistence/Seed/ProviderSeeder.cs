using MyCompany.Transfers.Domain.Providers;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MyCompany.Transfers.Infrastructure.Common.Persistence.Seed;

public static class ProviderSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Providers.AnyAsync(ct)) return;

        var fooPay = new Provider(
            id: "FooPay",
            name: "Foo Payments",
            baseUrl: "https://api.foopay.example",        // TODO: заменить
            timeoutSeconds: 30,
            authType: ProviderAuthType.Bearer,
            settingsJson: JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["token"] = "<FOOPAY_BEARER_TOKEN>"      // TODO: хранить в секретах
            })
        );

        var barNet = new Provider(
            id: "BarNet",
            name: "Bar Network",
            baseUrl: "https://gateway.barnet.example",    // TODO: заменить
            timeoutSeconds: 20,
            authType: ProviderAuthType.Hamac,
            settingsJson: JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["key"] = "<BARNET_KEY>",
                ["secret"] = "<BARNET_SECRET>",
                ["header"] = "X-Signature",
                ["algo"] = "HMACSHA256"
            })
        );

        db.Providers.AddRange(fooPay, barNet);
        await db.SaveChangesAsync(ct);
    }
}