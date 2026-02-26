using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Infrastructure.Persistence;

namespace MyCompany.Transfers.Infrastructure.Workers;

/// <summary>
/// Фоновый воркер для расчёта ежедневных остатков по агентам:
/// - в локальном часовом поясе банка (Scope = Local),
/// - в часовом поясе агента (Scope = Agent).
/// Использует историю операций AgentBalanceHistory.
/// </summary>
internal sealed class DailyBalanceWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    public DailyBalanceWorker(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var nowUtc = DateTime.UtcNow;
                var bankTimeZoneId = _configuration["Bank:TimeZoneId"] ?? "Asia/Dushanbe";

                await ProcessBankScopeAsync(db, nowUtc, bankTimeZoneId, stoppingToken);
                await ProcessAgentScopeAsync(db, nowUtc, stoppingToken);

                // Запускаем расчёт раз в час, при повторном запуске день пересчитывается идемпотентно.
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
            catch
            {
                // В случае ошибки делаем паузу и пробуем снова.
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private static async Task ProcessBankScopeAsync(
        AppDbContext db,
        DateTime nowUtc,
        string bankTimeZoneId,
        CancellationToken ct)
    {
        var tz = GetTimeZoneSafe(bankTimeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
        var targetDate = localNow.Date.AddDays(-1); // закрываем вчерашний день

        if (targetDate < DateTime.UnixEpoch.Date)
            return;

        var (utcStart, utcEnd) = ToUtcRange(targetDate, tz);

        var histories = await db.AgentBalanceHistories
            .AsNoTracking()
            .Where(h => h.CreatedAtUtc >= utcStart && h.CreatedAtUtc < utcEnd)
            .ToListAsync(ct);

        if (histories.Count == 0)
            return;

        var existing = await db.AgentDailyBalances
            .Where(d =>
                d.Date == targetDate &&
                d.TimeZoneId == bankTimeZoneId &&
                d.Scope == DailyBalanceScope.Local)
            .ToListAsync(ct);

        var existingMap = existing.ToDictionary(
            d => (d.AgentId, d.Currency),
            d => d);

        var groups = histories
            .GroupBy(h => (h.AgentId, h.Currency));

        foreach (var group in groups)
        {
            var ordered = group.OrderBy(h => h.CreatedAtUtc).ToList();
            var first = ordered.First();
            var last = ordered.Last();

            var opening = first.CurrentBalanceMinor;
            var closing = last.NewBalanceMinor;

            var key = (first.AgentId, first.Currency);

            if (existingMap.TryGetValue(key, out var daily))
            {
                daily.UpdateClosingBalance(closing);
            }
            else
            {
                daily = AgentDailyBalance.Create(
                    first.AgentId,
                    targetDate,
                    first.Currency,
                    opening,
                    closing,
                    bankTimeZoneId,
                    DailyBalanceScope.Local);

                db.AgentDailyBalances.Add(daily);
                existingMap[key] = daily;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task ProcessAgentScopeAsync(
        AppDbContext db,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var agents = await db.Agents.AsNoTracking().ToListAsync(ct);
        if (agents.Count == 0)
            return;

        foreach (var agent in agents)
        {
            var tz = GetTimeZoneSafe(agent.TimeZoneId);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
            var targetDate = localNow.Date.AddDays(-1);

            if (targetDate < DateTime.UnixEpoch.Date)
                continue;

            var (utcStart, utcEnd) = ToUtcRange(targetDate, tz);

            var histories = await db.AgentBalanceHistories
                .AsNoTracking()
                .Where(h =>
                    h.AgentId == agent.Id &&
                    h.CreatedAtUtc >= utcStart &&
                    h.CreatedAtUtc < utcEnd)
                .ToListAsync(ct);

            if (histories.Count == 0)
                continue;

            var existing = await db.AgentDailyBalances
                .Where(d =>
                    d.AgentId == agent.Id &&
                    d.Date == targetDate &&
                    d.TimeZoneId == agent.TimeZoneId &&
                    d.Scope == DailyBalanceScope.Agent)
                .ToListAsync(ct);

            var existingMap = existing.ToDictionary(
                d => d.Currency,
                d => d,
                StringComparer.OrdinalIgnoreCase);

            var groups = histories
                .GroupBy(h => h.Currency, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                var ordered = group.OrderBy(h => h.CreatedAtUtc).ToList();
                var first = ordered.First();
                var last = ordered.Last();

                var opening = first.CurrentBalanceMinor;
                var closing = last.NewBalanceMinor;
                var currency = first.Currency;

                if (existingMap.TryGetValue(currency, out var daily))
                {
                    daily.UpdateClosingBalance(closing);
                }
                else
                {
                    daily = AgentDailyBalance.Create(
                        agent.Id,
                        targetDate,
                        currency,
                        opening,
                        closing,
                        agent.TimeZoneId,
                        DailyBalanceScope.Agent);

                    db.AgentDailyBalances.Add(daily);
                    existingMap[currency] = daily;
                }
            }

            await db.SaveChangesAsync(ct);
        }
    }

    private static (DateTime utcStart, DateTime utcEnd) ToUtcRange(DateTime localDate, TimeZoneInfo tz)
    {
        var localStart = new DateTime(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var localEnd = localStart.AddDays(1);
        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, tz);
        return (utcStart, utcEnd);
    }

    private static TimeZoneInfo GetTimeZoneSafe(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}

