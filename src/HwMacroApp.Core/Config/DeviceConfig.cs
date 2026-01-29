using System.Text.Json.Serialization;
using HwMacroApp.Core.Devices;

namespace HwMacroApp.Core.Config;

public sealed class DeviceConfig
{
    [JsonPropertyName("devicePath")]
    public string DevicePath { get; set; } = "";

    [JsonPropertyName("friendlyName")]
    public string FriendlyName { get; set; } = "";

    [JsonPropertyName("connectionType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DeviceConnectionType ConnectionType { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("bindings")]
    public List<KeyBinding> Bindings { get; set; } = [];
}
