using System.Text.Json.Serialization;

namespace HwMacroApp.Core.Config;

public sealed class AppConfig
{
    [JsonPropertyName("devices")]
    public List<DeviceConfig> Devices { get; set; } = [];

    [JsonPropertyName("general")]
    public GeneralConfig General { get; set; } = new();
}
