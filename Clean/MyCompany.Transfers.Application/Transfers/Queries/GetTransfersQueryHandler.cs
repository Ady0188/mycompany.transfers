using MediatR;
using MyCompany.Transfers.Application.Common.Interfaces;
using MyCompany.Transfers.Application.Common.Models;
using MyCompany.Transfers.Application.Transfers.Dtos;

namespace MyCompany.Transfers.Application.Transfers.Queries;

public sealed class GetTransfersQueryHandler : IRequestHandler<GetTransfersQuery, PagedResult<TransferAdminDto>>
{
    private readonly ITransferReadRepository _transfers;
    private readonly IAgentRepository _agents;
    private readonly IServiceRepository _services;
    private readonly IProviderRepository _providers;

    public GetTransfersQueryHandler(
        ITransferReadRepository transfers,
        IAgentRepository agents,
        IServiceRepository services,
        IProviderRepository providers)
    {
        _transfers = transfers;
        _agents = agents;
        _services = services;
        _providers = providers;
    }

    public async Task<PagedResult<TransferAdminDto>> Handle(GetTransfersQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);
        var f = request.Filter;
        var (items, totalCount) = await _transfers.GetPagedForAdminAsync(
            page,
            pageSize,
            f?.Id,
            f?.AgentId,
            f?.ExternalId,
            f?.ProviderId,
            f?.ServiceId,
            f?.Status,
            f?.CreatedFrom,
            f?.CreatedTo,
            f?.Account,
            ct);

        var agentIds = items.Select(t => t.AgentId).Distinct().ToList();
        var serviceIds = items.Select(t => t.ServiceId).Distinct().ToList();
        var agents = await _agents.GetAllAsync(ct);
        var servicesList = await _services.GetAllAsync(ct);
        var providers = await _providers.GetAllAsync(ct);

        var agentNames = agents.Where(a => agentIds.Contains(a.Id)).ToDictionary(a => a.Id, a => a.Name);
        var serviceMap = servicesList.Where(s => serviceIds.Contains(s.Id)).ToDictionary(s => s.Id, s => (s.Name, s.ProviderId));
        var providerNames = providers.ToDictionary(p => p.Id, p => p.Name);

        var dtos = items.Select(t =>
        {
            var serviceInfo = serviceMap.GetValueOrDefault(t.ServiceId);
            var providerName = serviceInfo.ProviderId != null && providerNames.TryGetValue(serviceInfo.ProviderId, out var pn) ? pn : null;
            return TransferAdminDto.FromDomain(
                t,
                agentNames.GetValueOrDefault(t.AgentId),
                serviceInfo.Name,
                providerName);
        }).ToList();

        return new PagedResult<TransferAdminDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
