namespace MyCompany.Transfers.Admin.Client.Models;

public sealed class ProviderSettings
{
    public Dictionary<string, ProviderOperationSettings> Operations { get; set; } = new();
    public Dictionary<OutboxStatus, string> JobScenario { get; set; } = new();
    public Dictionary<string, string> Common { get; set; } = new();
}
