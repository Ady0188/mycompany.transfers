using System.Collections.Generic;

namespace MyCompany.Transfers.Domain.Agents;

/// <summary>
/// Настройки агента, хранящиеся в поле Agent.SettingsJson.
/// Используются для управления доступными данным в ответах команд/запросов
/// и для произвольных пользовательских настроек (key/value).
/// </summary>
public sealed class AgentSettings
{
    /// <summary>
    /// Настройка возвращаемых параметров по операциям (Command/Query).
    /// Ключ — код операции (например, "Check"), значение — список кодов параметров (ParamDefinition.Code),
    /// которые разрешено возвращать агенту в ответе.
    /// Если для операции нет записи, используются все параметры услуги.
    /// </summary>
    public Dictionary<string, string[]> ResponseParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Произвольные настройки агента (key/value), которые могут использоваться
    /// в различных сценариях (например, шаблоны писем, дополнительные флаги и т.п.).
    /// </summary>
    public Dictionary<string, string> Common { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

