using System.Text.Json.Serialization;

namespace HwMacroApp.Core.Config;

public sealed class KeyBinding
{
    [JsonPropertyName("vkey")]
    public ushort VKey { get; set; }

    [JsonPropertyName("keyName")]
    public string KeyName { get; set; } = "";

    [JsonPropertyName("actions")]
    public List<ActionConfig> Actions { get; set; } = [];
}
