using System.Text.Json.Serialization;

namespace MyCompany.Transfers.Admin.Client.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OutboxStatus
{
    TO_SEND,
    SENDING,
    STATUS,
    SUCCESS,
    TECHNICAL,
    FAILED,
    EXPIRED,
    FRAUD,
    NORESPONSE,
    SETTING
}
