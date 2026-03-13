using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyCompany.Transfers.Api.Auth;
using MyCompany.Transfers.Api.Models;
using MyCompany.Transfers.Application.Common.Models;
using MyCompany.Transfers.Domain.Agents;
using MyCompany.Transfers.Infrastructure.Persistence;

namespace MyCompany.Transfers.Api.Controllers;

/// <summary>
/// История балансов и дневные балансы агентов для админ-панели.
/// </summary>
[ApiController]
[Route("api/admin/balances")]
[Consumes("application/json")]
[Produces("application/json", "application/problem+json")]
[ApiExplorerSettings(GroupName = "admin")]
[AdminRoleAuthorize]
public sealed class BalancesController : BaseController
{
    private readonly AppDbContext _db;

    public BalancesController(AppDbContext db) => _db = db;

    /// <summary>
    /// История изменений баланса агентов (AgentBalanceHistory) с фильтрами и пагинацией.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? agentId,
        [FromQuery] string? terminalId,
        [FromQuery] string? currency,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 50;
        pageSize = Math.Min(pageSize, 200);

        var query = _db.AgentBalanceHistories.AsNoTracking().AsQueryable();

        if (from.HasValue)
        {
            var fromUtc = from.Value.UtcDateTime;
            query = query.Where(h => h.CreatedAtUtc >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = to.Value.UtcDateTime;
            query = query.Where(h => h.CreatedAtUtc <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(agentId))
            query = query.Where(h => h.AgentId == agentId);

        if (!string.IsNullOrWhiteSpace(terminalId))
            query = query.Where(h => h.TerminalId == terminalId);

        if (!string.IsNullOrWhiteSpace(currency))
            query = query.Where(h => h.Currency == currency);

        var total = await query.LongCountAsync(ct);

        var items = await query
            .OrderByDescending(h => h.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new AgentBalanceHistoryItemDto
            {
                CreatedAtUtc = h.CreatedAtUtc,
                AgentId = h.AgentId,
                TerminalId = h.TerminalId ?? string.Empty,
                Currency = h.Currency,
                CurrentBalanceMinor = h.CurrentBalanceMinor,
                NewBalanceMinor = h.NewBalanceMinor,
                ChangeMinor = h.IncomeMinor,
                ReferenceType = h.ReferenceType.ToString(),
                ReferenceId = h.ReferenceId
            })
            .ToListAsync(ct);

        var result = new PagedResult<AgentBalanceHistoryItemDto>
        {
            Items = items,
            TotalCount = (int)total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(result);
    }

    /// <summary>
    /// Дневные балансы агентов (AgentDailyBalance) с фильтрами и пагинацией.
    /// </summary>
    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? agentId,
        [FromQuery] string? terminalId,
        [FromQuery] string? currency,
        [FromQuery] string? timeZoneId,
        [FromQuery] DailyBalanceScope? scope,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 50;
        pageSize = Math.Min(pageSize, 200);

        DateTime? fromDate = from?.UtcDateTime.Date;
        DateTime? toDate = to?.UtcDateTime.Date;

        var query = _db.AgentDailyBalances.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(d => d.Date >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(d => d.Date <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(agentId))
            query = query.Where(d => d.AgentId == agentId);

        if (!string.IsNullOrWhiteSpace(terminalId))
            query = query.Where(d => d.TerminalId == terminalId);

        if (!string.IsNullOrWhiteSpace(currency))
            query = query.Where(d => d.Currency == currency);

        if (!string.IsNullOrWhiteSpace(timeZoneId))
            query = query.Where(d => d.TimeZoneId == timeZoneId);

        if (scope.HasValue)
            query = query.Where(d => d.Scope == scope.Value);

        var total = await query.LongCountAsync(ct);

        var paged = await query
            .OrderByDescending(d => d.Date)
            .ThenBy(d => d.AgentId)
            .ThenBy(d => d.TerminalId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(
                _db.Agents.AsNoTracking(),
                d => d.AgentId,
                a => a.Id,
                (d, a) => new { Daily = d, AgentName = a.Name })
            .ToListAsync(ct);

        var items = paged.Select(x => new AgentDailyBalanceItemDto
        {
            Date = x.Daily.Date,
            AgentId = x.Daily.AgentId,
            AgentName = x.AgentName,
            TerminalId = x.Daily.TerminalId ?? string.Empty,
            Currency = x.Daily.Currency,
            OpeningBalanceMinor = x.Daily.OpeningBalanceMinor,
            ClosingBalanceMinor = x.Daily.ClosingBalanceMinor,
            TimeZoneId = x.Daily.TimeZoneId,
            Scope = x.Daily.Scope
        }).ToList();

        var result = new PagedResult<AgentDailyBalanceItemDto>
        {
            Items = items,
            TotalCount = (int)total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(result);
    }
}

