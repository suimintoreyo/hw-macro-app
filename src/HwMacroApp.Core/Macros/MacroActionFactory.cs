using HwMacroApp.Core.Config;

namespace HwMacroApp.Core.Macros;

/// <summary>ActionConfig から IMacroAction を生成する。</summary>
public sealed class MacroActionFactory
{
    public IMacroAction Create(ActionConfig config)
    {
        return config.Type.ToLowerInvariant() switch
        {
            "keystroke" => new KeyStrokeAction
            {
                Keys = config.Keys?.Select(k => (ushort)k).ToArray() ?? [],
                Modifiers = config.Modifiers?.Select(k => (ushort)k).ToArray() ?? [],
            },
            "launchapp" => new LaunchAppAction
            {
                Path = config.Path ?? throw new InvalidOperationException("LaunchApp requires Path"),
                Arguments = config.Args ?? "",
            },
            "script" => new ScriptAction
            {
                ScriptPath = config.Path ?? throw new InvalidOperationException("Script requires Path"),
            },
            "delay" => new DelayAction
            {
                Milliseconds = config.Milliseconds ?? 100,
            },
            _ => throw new NotSupportedException($"Unknown action type: {config.Type}"),
        };
    }
}
