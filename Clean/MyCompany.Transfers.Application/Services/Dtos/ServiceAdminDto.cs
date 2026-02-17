using MyCompany.Transfers.Domain.Services;

namespace MyCompany.Transfers.Application.Services.Dtos;

public sealed record ServiceParamDefinitionDto(string ParameterId, bool Required);

public sealed record ServiceAdminDto(
    string Id,
    string ProviderId,
    string ProviderServiceId,
    string Name,
    string[] AllowedCurrencies,
    string? FxRounding,
    long MinAmountMinor,
    long MaxAmountMinor,
    Guid AccountDefinitionId,
    List<ServiceParamDefinitionDto> Parameters)
{
    public static ServiceAdminDto FromDomain(Service s) =>
        new(
            s.Id,
            s.ProviderId,
            s.ProviderServiceId,
            s.Name,
            s.AllowedCurrencies,
            s.FxRounding,
            s.MinAmountMinor,
            s.MaxAmountMinor,
            s.AccountDefinitionId,
            s.Parameters.Select(p => new ServiceParamDefinitionDto(p.ParameterId, p.Required)).ToList());
}
