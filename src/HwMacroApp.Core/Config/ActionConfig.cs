using System.Text.Json.Serialization;

namespace HwMacroApp.Core.Config;

public sealed class ActionConfig
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("keys")]
    public int[]? Keys { get; set; }

    [JsonPropertyName("modifiers")]
    public int[]? Modifiers { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("args")]
    public string? Args { get; set; }

    [JsonPropertyName("ms")]
    public int? Milliseconds { get; set; }
}
