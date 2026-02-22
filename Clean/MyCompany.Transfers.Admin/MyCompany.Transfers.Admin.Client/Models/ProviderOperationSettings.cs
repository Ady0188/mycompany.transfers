namespace MyCompany.Transfers.Admin.Client.Models;

public sealed class ProviderOperationSettings
{
    public string Method { get; set; } = "POST";
    public Dictionary<string, string> HeaderTemplate { get; set; } = new();
    public string PathTemplate { get; set; } = "/";
    public string? BodyTemplate { get; set; }
    public string Format { get; set; } = "json";
    public string ResponseFormat { get; set; } = "json";
    public string? ResponseField { get; set; }
    public string? SuccessField { get; set; }
    public string? SuccessValue { get; set; }
    public string? ErrorField { get; set; }
}
