using Microsoft.EntityFrameworkCore;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Reports.Transfers;
using MyCompany.Transfers.Domain.Transfers;
using MyCompany.Transfers.Infrastructure.Persistence;

namespace MyCompany.Transfers.Infrastructure.Reports;

public sealed class TransfersReportService : ITransfersReportService
{
    private readonly AppDbContext _db;

    public TransfersReportService(AppDbContext db) => _db = db;

    public async Task<TransfersReportResult<TransfersByPeriodReportItemDto>> GetByPeriodAsync(
        TransfersCommonReportFilter filter,
        TransfersReportGroupBy groupBy,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = ApplyCommonFilter(_db.Transfers.AsNoTracking(), filter);
        var pageIndex = Math.Max(0, page - 1);
        var size = pageSize <= 0 ? TransfersReportLimits.DefaultPageSize : Math.Min(pageSize, TransfersReportLimits.MaxPageSize);

        if (groupBy == TransfersReportGroupBy.Month)
        {
            var grouped = query
                .GroupBy(t => new { t.CreatedAtUtc.Year, t.CreatedAtUtc.Month, Currency = t.Amount.Currency })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Currency = g.Key.Currency,
                    Count = g.LongCount(),
                    AmountMinor = g.Sum(x => x.Amount.Minor),
                    FeeMinor = g.Sum(x => x.CurrentQuote != null ? x.CurrentQuote.Fee.Minor : 0),
                    FeeCurrency = g.Select(x => x.CurrentQuote != null ? x.CurrentQuote.Fee.Currency : g.Key.Currency).FirstOrDefault() ?? g.Key.Currency,
                    ProviderFeeMinor = g.Sum(x => x.CurrentQuote != null ? x.CurrentQuote.ProviderFee.Minor : 0),
                    ProviderFeeCurrency = g.Select(x => x.CurrentQuote != null ? x.CurrentQuote.ProviderFee.Currency : g.Key.Currency).FirstOrDefault() ?? g.Key.Currency
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month);

            var totalCount = await grouped.CountAsync(ct);
            var itemsRaw = await grouped
                .Skip(pageIndex * size)
                .Take(size)
                .ToListAsync(ct);

            var items = itemsRaw.Select(x =>
            {
                var start = new DateTime(x.Year, x.Month, 1);
                var end = start.AddMonths(1).AddTicks(-1);
                return new TransfersByPeriodReportItemDto(
                    start,
                    end,
                    x.Count,
                    x.AmountMinor,
                    x.Currency,
                    x.FeeMinor,
                    x.FeeCurrency,
                    x.ProviderFeeMinor,
                    x.ProviderFeeCurrency);
            }).ToList();

            var totals = await query.ToListAsync(ct);
            var totalAmount = totals.Sum(t => t.Amount.Minor);
            var totalFee = totals.Sum(t => t.CurrentQuote != null ? t.CurrentQuote.Fee.Minor : 0);
            var totalProviderFee = totals.Sum(t => t.CurrentQuote != null ? t.CurrentQuote.ProviderFee.Minor : 0);

            return new TransfersReportResult<TransfersByPeriodReportItemDto>(
                items,
                totalCount,
                totalAmount,
                totalFee,
                totalProviderFee);
        }
        else
        {
            var grouped = query
                .GroupBy(t => new { Date = t.CreatedAtUtc.Date, Currency = t.Amount.Currency })
                .Select(g => new
                {
                    g.Key.Date,
                    Currency = g.Key.Currency,
                    Count = g.LongCount(),
                    AmountMinor = g.Sum(x => x.Amount.Minor),
                    FeeMinor = g.Sum(x => x.CurrentQuote != null ? x.CurrentQuote.Fee.Minor : 0),
                    FeeCurrency = g.Select(x => x.CurrentQuote != null ? x.CurrentQuote.Fee.Currency : g.Key.Currency).FirstOrDefault() ?? g.Key.Currency,
                    ProviderFeeMinor = g.Sum(x => x.CurrentQuote != null ? x.CurrentQuote.ProviderFee.Minor : 0),
                    ProviderFeeCurrency = g.Select(x => x.CurrentQuote != null ? x.CurrentQuote.ProviderFee.Currency : g.Key.Currency).FirstOrDefault() ?? g.Key.Currency
                })
                .OrderBy(g => g.Date);

            var totalCount = await grouped.CountAsync(ct);
            var itemsRaw = await grouped
                .Skip(pageIndex * size)
                .Take(size)
                .ToListAsync(ct);

            var items = itemsRaw.Select(x =>
            {
                var start = x.Date;
                var end = x.Date.AddDays(1).AddTicks(-1);
                return new TransfersByPeriodReportItemDto(
                    start,
                    end,
                    x.Count,
                    x.AmountMinor,
                    x.Currency,
                    x.FeeMinor,
                    x.FeeCurrency,
                    x.ProviderFeeMinor,
                    x.ProviderFeeCurrency);
            }).ToList();

            var totals = await query.ToListAsync(ct);
            var totalAmount = totals.Sum(t => t.Amount.Minor);
            var totalFee = totals.Sum(t => t.CurrentQuote != null ? t.CurrentQuote.Fee.Minor : 0);
            var totalProviderFee = totals.Sum(t => t.CurrentQuote != null ? t.CurrentQuote.ProviderFee.Minor : 0);

            return new TransfersReportResult<TransfersByPeriodReportItemDto>(
                items,
                totalCount,
                totalAmount,
                totalFee,
                totalProviderFee);
        }
    }

    public async Task<TransfersReportResult<TransfersByAgentReportItemDto>> GetByAgentAsync(
        TransfersCommonReportFilter filter,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = ApplyCommonFilter(_db.Transfers.AsNoTracking(), filter);

        var grouped = query
            .GroupBy(t => new { t.AgentId, Currency = t.Amount.Currency })
            .Select(g => new
            {
                g.Key.AgentId,
                Currency = g.Key.Currency,
                Count = g.LongCount(),
                AmountMinor = g.Sum(x => x.Amount.Minor),
                FeeMinor = g.Sum(x => x.CurrentQuote != null ? x.CurrentQuote.Fee.Minor : 0),
                FeeCurrency = g.Select(x => x.CurrentQuote != null ? x.CurrentQuote.Fee.Currency : g.Key.Currency).FirstOrDefault() ?? g.Key.Currency,
                ProviderFeeMinor = g.Sum(x => x.CurrentQuote != null ? x.CurrentQuote.ProviderFee.Minor : 0),
                ProviderFeeCurrency = g.Select(x => x.CurrentQuote != null ? x.CurrentQuote.ProviderFee.Currency : g.Key.Currency).FirstOrDefault() ?? g.Key.Currency
            })
            .OrderBy(x => x.AgentId);

        var totalCount = await grouped.CountAsync(ct);
        var pageIndex = Math.Max(0, page - 1);
        var size = pageSize <= 0 ? TransfersReportLimits.DefaultPageSize : Math.Min(pageSize, TransfersReportLimits.MaxPageSize);
        var itemsRaw = await grouped
            .Skip(pageIndex * size)
            .Take(size)
            .ToListAsync(ct);

        var agentIds = itemsRaw.Select(x => x.AgentId).Distinct().ToList();
        var agents = await _db.Agents
            .Where(a => agentIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Name })
            .ToListAsync(ct);
        var agentNames = agents.ToDictionary(a => a.Id, a => a.Name);

        var items = itemsRaw.Select(x =>
            new TransfersByAgentReportItemDto(
                x.AgentId,
                agentNames.GetValueOrDefault(x.AgentId),
                x.Count,
                x.AmountMinor,
                x.Currency,
                x.FeeMinor,
                x.FeeCurrency,
                x.ProviderFeeMinor,
                x.ProviderFeeCurrency)).ToList();

        var totals = await query.ToListAsync(ct);
        var totalAmount = totals.Sum(t => t.Amount.Minor);
        var totalFee = totals.Sum(t => t.CurrentQuote != null ? t.CurrentQuote.Fee.Minor : 0);
        var totalProviderFee = totals.Sum(t => t.CurrentQuote != null ? t.CurrentQuote.ProviderFee.Minor : 0);

        return new TransfersReportResult<TransfersByAgentReportItemDto>(
            items,
            totalCount,
            totalAmount,
            totalFee,
            totalProviderFee);
    }

    public async Task<TransfersReportResult<TransfersByProviderReportItemDto>> GetByProviderAsync(
        TransfersCommonReportFilter filter,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = ApplyCommonFilter(_db.Transfers.AsNoTracking(), filter);

        var joined = from t in query
                     join s in _db.Services.AsNoTracking() on t.ServiceId equals s.Id
                     select new { t, s.ProviderId, Currency = t.Amount.Currency };

        var grouped = joined
            .GroupBy(x => new { x.ProviderId, x.Currency })
            .Select(g => new
            {
                g.Key.ProviderId,
                Currency = g.Key.Currency,
                Count = g.LongCount(),
                AmountMinor = g.Sum(x => x.t.Amount.Minor),
                FeeMinor = g.Sum(x => x.t.CurrentQuote != null ? x.t.CurrentQuote.Fee.Minor : 0),
                FeeCurrency = g.Select(x => x.t.CurrentQuote != null ? x.t.CurrentQuote.Fee.Currency : g.Key.Currency).FirstOrDefault() ?? g.Key.Currency,
                ProviderFeeMinor = g.Sum(x => x.t.CurrentQuote != null ? x.t.CurrentQuote.ProviderFee.Minor : 0),
                ProviderFeeCurrency = g.Select(x => x.t.CurrentQuote != null ? x.t.CurrentQuote.ProviderFee.Currency : g.Key.Currency).FirstOrDefault() ?? g.Key.Currency
            })
            .OrderBy(x => x.ProviderId);

        var totalCount = await grouped.CountAsync(ct);
        var pageIndex = Math.Max(0, page - 1);
        var size = pageSize <= 0 ? TransfersReportLimits.DefaultPageSize : Math.Min(pageSize, TransfersReportLimits.MaxPageSize);
        var itemsRaw = await grouped
            .Skip(pageIndex * size)
            .Take(size)
            .ToListAsync(ct);

        var providerIds = itemsRaw.Select(x => x.ProviderId).Distinct().ToList();
        var providers = await _db.Providers
            .Where(p => providerIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(ct);
        var providerNames = providers.ToDictionary(p => p.Id, p => p.Name);

        var items = itemsRaw.Select(x =>
            new TransfersByProviderReportItemDto(
                x.ProviderId ?? string.Empty,
                x.ProviderId != null && providerNames.TryGetValue(x.ProviderId, out var name) ? name : null,
                x.Count,
                x.AmountMinor,
                x.Currency,
                x.FeeMinor,
                x.FeeCurrency,
                x.ProviderFeeMinor,
                x.ProviderFeeCurrency)).ToList();

        var allJoined = await joined.ToListAsync(ct);
        var totalAmount = allJoined.Sum(x => x.t.Amount.Minor);
        var totalFee = allJoined.Sum(x => x.t.CurrentQuote != null ? x.t.CurrentQuote.Fee.Minor : 0);
        var totalProviderFee = allJoined.Sum(x => x.t.CurrentQuote != null ? x.t.CurrentQuote.ProviderFee.Minor : 0);

        return new TransfersReportResult<TransfersByProviderReportItemDto>(
            items,
            totalCount,
            totalAmount,
            totalFee,
            totalProviderFee);
    }

    public async Task<TransfersReportResult<TransfersRevenueReportItemDto>> GetRevenueAsync(
        TransfersCommonReportFilter filter,
        TransfersReportGroupBy groupBy,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var query = ApplyCommonFilter(_db.Transfers.AsNoTracking(), filter);
        var pageIndex = Math.Max(0, page - 1);
        var size = pageSize <= 0 ? TransfersReportLimits.DefaultPageSize : Math.Min(pageSize, TransfersReportLimits.MaxPageSize);

        if (groupBy == TransfersReportGroupBy.Month)
        {
            var grouped = query
                .GroupBy(t => new { t.CreatedAtUtc.Year, t.CreatedAtUtc.Month, Currency = t.Amount.Currency })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Currency = g.Key.Currency,
                    Count = g.LongCount(),
                    TotalFeeMinor = g.Sum(x => x.CurrentQuote != null ? x.CurrentQuote.Fee.Minor : 0),
                    ProviderFeeMinor = g.Sum(x => x.CurrentQuote != null ? x.CurrentQuote.ProviderFee.Minor : 0)
                })
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Month);

            var totalCount = await grouped.CountAsync(ct);
            var itemsRaw = await grouped
                .Skip(pageIndex * size)
                .Take(size)
                .ToListAsync(ct);

            var items = itemsRaw.Select(x =>
            {
                var start = new DateTime(x.Year, x.Month, 1);
                var end = start.AddMonths(1).AddTicks(-1);
                var margin = x.TotalFeeMinor - x.ProviderFeeMinor;
                return new TransfersRevenueReportItemDto(start, end, x.Count, x.TotalFeeMinor, x.ProviderFeeMinor, margin, x.Currency);
            }).ToList();

            var totals = await query.ToListAsync(ct);
            var totalFee = totals.Sum(t => t.CurrentQuote != null ? t.CurrentQuote.Fee.Minor : 0);
            var totalProviderFee = totals.Sum(t => t.CurrentQuote != null ? t.CurrentQuote.ProviderFee.Minor : 0);

            return new TransfersReportResult<TransfersRevenueReportItemDto>(
                items,
                totalCount,
                0,
                totalFee,
                totalProviderFee);
        }
        else
        {
            var grouped = query
                .GroupBy(t => new { Date = t.CreatedAtUtc.Date, Currency = t.Amount.Currency })
                .Select(g => new
                {
                    g.Key.Date,
                    Currency = g.Key.Currency,
                    Count = g.LongCount(),
                    TotalFeeMinor = g.Sum(x => x.CurrentQuote != null ? x.CurrentQuote.Fee.Minor : 0),
                    ProviderFeeMinor = g.Sum(x => x.CurrentQuote != null ? x.CurrentQuote.ProviderFee.Minor : 0)
                })
                .OrderBy(g => g.Date);

            var totalCount = await grouped.CountAsync(ct);
            var itemsRaw = await grouped
                .Skip(pageIndex * size)
                .Take(size)
                .ToListAsync(ct);

            var items = itemsRaw.Select(x =>
            {
                var start = x.Date;
                var end = x.Date.AddDays(1).AddTicks(-1);
                var margin = x.TotalFeeMinor - x.ProviderFeeMinor;
                return new TransfersRevenueReportItemDto(start, end, x.Count, x.TotalFeeMinor, x.ProviderFeeMinor, margin, x.Currency);
            }).ToList();

            var totals = await query.ToListAsync(ct);
            var totalFee = totals.Sum(t => t.CurrentQuote != null ? t.CurrentQuote.Fee.Minor : 0);
            var totalProviderFee = totals.Sum(t => t.CurrentQuote != null ? t.CurrentQuote.ProviderFee.Minor : 0);

            return new TransfersReportResult<TransfersRevenueReportItemDto>(
                items,
                totalCount,
                0,
                totalFee,
                totalProviderFee);
        }
    }

    public async Task<TransfersReportResult<TransfersByBankReportItemDto>> GetByBankAsync(
        TransfersCommonReportFilter filter,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var providerIds = new[] { "IPS", "FIMI" };

        var baseQuery =
            from t in _db.Transfers.AsNoTracking()
            join s in _db.Services.AsNoTracking() on t.ServiceId equals s.Id
            where providerIds.Contains(s.ProviderId)
            select t;

        var query = ApplyCommonFilter(baseQuery, filter, ignoreStatus: true);
        var transfers = await query.ToListAsync(ct);

        var bins = await _db.Bins.AsNoTracking().ToListAsync(ct);

        static (string code, string name) ResolveBank(IReadOnlyList<MyCompany.Transfers.Domain.Bins.Bin> allBins, string? account)
        {
            if (string.IsNullOrWhiteSpace(account))
                return ("UNKNOWN", "Неизвестный банк");
            var pan = account.Trim();
            MyCompany.Transfers.Domain.Bins.Bin? best = null;
            foreach (var b in allBins)
            {
                if (pan.Length < b.Len) continue;
                if (pan.StartsWith(b.Prefix, StringComparison.Ordinal))
                {
                    if (best == null || b.Len > best.Len)
                        best = b;
                }
            }
            return best is null ? ("UNKNOWN", "Неизвестный банк") : (best.Code, best.Name);
        }

        var dict = new Dictionary<(string BankCode, string BankName, string Currency), (long SuccCount, long SuccAmount, long ErrCount, long ErrAmount, long TotalCount, long TotalAmount)>();

        foreach (var t in transfers)
        {
            var (bankCode, bankName) = ResolveBank(bins, t.Account);
            var key = (bankCode, bankName, t.Amount.Currency);
            dict.TryGetValue(key, out var agg);

            var amount = t.Amount.Minor;
            var status = t.Status;
            var isSuccess = status == TransferStatus.SUCCESS;
            var isError = status == TransferStatus.FAILED || status == TransferStatus.TECHNICAL;

            if (isSuccess)
            {
                agg.SuccCount++;
                agg.SuccAmount += amount;
            }
            else if (isError)
            {
                agg.ErrCount++;
                agg.ErrAmount += amount;
            }

            agg.TotalCount++;
            agg.TotalAmount += amount;

            dict[key] = agg;
        }

        var pageIndex = Math.Max(0, page - 1);
        var size = pageSize <= 0 ? TransfersReportLimits.DefaultPageSize : Math.Min(pageSize, TransfersReportLimits.MaxPageSize);

        var itemsAll = dict
            .Select(kv => new TransfersByBankReportItemDto(
                kv.Key.BankCode,
                kv.Key.BankName,
                kv.Key.Currency,
                kv.Value.SuccCount,
                kv.Value.SuccAmount,
                kv.Value.ErrCount,
                kv.Value.ErrAmount,
                kv.Value.TotalCount,
                kv.Value.TotalAmount))
            .OrderBy(i => i.BankName)
            .ThenBy(i => i.Currency)
            .ToList();

        var totalCount = itemsAll.Count;
        var items = itemsAll
            .Skip(pageIndex * size)
            .Take(size)
            .ToList();

        var totalAmount = itemsAll.Sum(i => i.TotalAmountMinor);

        return new TransfersReportResult<TransfersByBankReportItemDto>(
            items,
            totalCount,
            totalAmount,
            0,
            0);
    }

    private IQueryable<Transfer> ApplyCommonFilter(IQueryable<Transfer> query, TransfersCommonReportFilter filter, bool ignoreStatus = false)
    {
        if (filter.From.HasValue)
            query = query.Where(t => t.CreatedAtUtc >= filter.From.Value);
        if (filter.To.HasValue)
            query = query.Where(t => t.CreatedAtUtc <= filter.To.Value);
        if (!ignoreStatus && !string.IsNullOrWhiteSpace(filter.Status))
        {
            var st = filter.Status.Trim();
            query = query.Where(t => t.Status.ToString() == st);
        }
        if (!string.IsNullOrWhiteSpace(filter.AgentId))
        {
            var aId = filter.AgentId.Trim();
            query = query.Where(t => t.AgentId == aId);
        }
        if (!string.IsNullOrWhiteSpace(filter.ProviderId))
        {
            var pId = filter.ProviderId.Trim();
            query = query.Where(t => _db.Services.Any(s => s.Id == t.ServiceId && s.ProviderId == pId));
        }
        if (!string.IsNullOrWhiteSpace(filter.ServiceId))
        {
            var sId = filter.ServiceId.Trim();
            query = query.Where(t => t.ServiceId == sId);
        }
        if (!string.IsNullOrWhiteSpace(filter.AmountCurrency))
        {
            var cur = filter.AmountCurrency.Trim();
            query = query.Where(t => t.Amount.Currency == cur);
        }
        return query;
    }
}

