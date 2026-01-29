using System.Text.Json.Serialization;

namespace HwMacroApp.Core.Config;

public sealed class GeneralConfig
{
    [JsonPropertyName("startWithWindows")]
    public bool StartWithWindows { get; set; }

    [JsonPropertyName("showNotificationOnMacro")]
    public bool ShowNotificationOnMacro { get; set; }
}
