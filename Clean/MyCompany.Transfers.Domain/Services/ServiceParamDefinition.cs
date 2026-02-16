namespace MyCompany.Transfers.Domain.Services;

public sealed class ServiceParamDefinition
{
    public string ServiceId { get; private set; } = default!;
    public string ParameterId { get; private set; } = default!;
    public bool Required { get; private set; }
    public ParamDefinition? Parameter { get; private set; }

    private ServiceParamDefinition() { }

    public ServiceParamDefinition(string serviceId, string parameterId, bool required)
    {
        ServiceId = serviceId;
        ParameterId = parameterId;
        Required = required;
    }
}
