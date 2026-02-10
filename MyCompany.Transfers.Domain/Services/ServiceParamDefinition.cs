using System.Reflection.Metadata;

namespace MyCompany.Transfers.Domain.Services;

public sealed class ServiceParamDefinition111
{
    public string Code { get; set; } = default!;
    public bool Required { get; set; }
    public string? Regex { get; set; }
}

public sealed class ServiceParamDefinition
{
    public string ServiceId { get; private set; } = default!;
    public string ParameterId { get; private set; } = default!;
    public bool Required { get; private set; }

    // навигация (опционально)
    public ParamDefinition Parameter { get; private set; } = default!;

    private ServiceParamDefinition() { }

    public ServiceParamDefinition(string serviceId, string parameterId, bool required)
    {
        ServiceId = serviceId;
        ParameterId = parameterId;
        Required = required;
    }
}