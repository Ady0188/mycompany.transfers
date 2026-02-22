namespace MyCompany.Transfers.Admin.Client.Models;

/// <summary>Один элемент настроек агента (ключ — значение).</summary>
public sealed class SettingItem
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}
