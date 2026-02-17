using ErrorOr;
using MediatR;
using MyCompany.Transfers.Application.Common.Helpers;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Rates.Dtos;
using MyCompany.Transfers.Domain.Rates;

namespace MyCompany.Transfers.Application.Rates.Commands;

/// <summary>
/// Добавить новый курс валют или обновить существующий (по ключу AgentId + BaseCurrency + QuoteCurrency).
/// </summary>
public sealed record UpsertFxRateCommand(
    string AgentId,
    string BaseCurrency,
    string QuoteCurrency,
    decimal Rate,
    string Source = "manual") : IRequest<ErrorOr<FxRateAdminDto>>;

public sealed class UpsertFxRateCommandHandler : IRequestHandler<UpsertFxRateCommand, ErrorOr<FxRateAdminDto>>
{
    private readonly IFxRateRepository _fxRates;
    private readonly IAgentReadRepository _agents;
    private readonly IUnitOfWork _uow;
    private readonly TimeProvider _clock;

    public UpsertFxRateCommandHandler(
        IFxRateRepository fxRates,
        IAgentReadRepository agents,
        IUnitOfWork uow,
        TimeProvider clock)
    {
        _fxRates = fxRates;
        _agents = agents;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ErrorOr<FxRateAdminDto>> Handle(UpsertFxRateCommand cmd, CancellationToken ct)
    {
        var baseCcy = cmd.BaseCurrency.Trim().ToUpperInvariant();
        var quoteCcy = cmd.QuoteCurrency.Trim().ToUpperInvariant();
        if (baseCcy == quoteCcy)
            return AppErrors.Common.Validation("Base и Quote валюты не должны совпадать.");

        if (!await _agents.ExistsAsync(cmd.AgentId, ct))
            return AppErrors.Common.Validation($"Агент '{cmd.AgentId}' не найден.");

        var now = _clock.GetUtcNow();
        var existing = await _fxRates.GetForUpdateAsync(cmd.AgentId, baseCcy, quoteCcy, ct);

        if (existing != null)
        {
            existing.UpdateRate(cmd.Rate, now, cmd.Source);
            await _uow.ExecuteTransactionalAsync(_ =>
            {
                _fxRates.Update(existing);
                return Task.FromResult(true);
            }, ct);
            return FxRateAdminDto.FromDomain(existing);
        }

        var entity = FxRate.Create(cmd.AgentId, baseCcy, quoteCcy, cmd.Rate, now, cmd.Source, true);
        await _uow.ExecuteTransactionalAsync(_ =>
        {
            _fxRates.Add(entity);
            return Task.FromResult(true);
        }, ct);
        return FxRateAdminDto.FromDomain(entity);
    }
}
