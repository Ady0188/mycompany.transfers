using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

public sealed class DashboardOverviewModel
{
    [JsonPropertyName("transfersToday")]
    public long TransfersToday { get; set; }

    [JsonPropertyName("transfersLast7Days")]
    public long TransfersLast7Days { get; set; }

    [JsonPropertyName("revenueLast7DaysMinor")]
    public long RevenueLast7DaysMinor { get; set; }

    [JsonPropertyName("revenueCurrency")]
    public string RevenueCurrency { get; set; } = "";

    [JsonPropertyName("last14DaysLabels")]
    public List<string> Last14DaysLabels { get; set; } = new();

    [JsonPropertyName("last14DaysTransfers")]
    public List<long> Last14DaysTransfers { get; set; } = new();

    [JsonPropertyName("last14DaysRevenueMinor")]
    public List<long> Last14DaysRevenueMinor { get; set; } = new();

    [JsonPropertyName("topProvidersLabels")]
    public List<string> TopProvidersLabels { get; set; } = new();

    [JsonPropertyName("topProvidersCounts")]
    public List<long> TopProvidersCounts { get; set; } = new();
}

