using HwMacroApp.Core.Config;
using HwMacroApp.Core.Devices;

namespace HwMacroApp.Core.Macros;

/// <summary>
/// RawKeyEvent を受け取り、設定に基づいてマクロを実行する。
/// </summary>
public sealed class MacroDispatcher
{
    private readonly AppConfig _config;
    private readonly MacroActionFactory _factory = new();

    public MacroDispatcher(AppConfig config)
    {
        _config = config;
    }

    /// <summary>キーイベントを処理し、マクロがあれば実行する。</summary>
    public async Task HandleKeyEventAsync(RawKeyEvent evt)
    {
        if (evt.IsKeyUp) return;

        var device = _config.Devices.FirstOrDefault(
            d => d.Enabled && d.DevicePath.Equals(evt.DevicePath, StringComparison.OrdinalIgnoreCase));

        if (device == null) return;

        var binding = device.Bindings.FirstOrDefault(
            b => b.VKey == evt.VKey);

        if (binding == null) return;

        var actions = binding.Actions.Select(_factory.Create).ToList();
        var sequence = new MacroSequence { Actions = actions };

        await sequence.ExecuteAsync();
    }
}
