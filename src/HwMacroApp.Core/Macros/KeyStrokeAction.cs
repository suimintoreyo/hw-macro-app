using System.Runtime.InteropServices;
using HwMacroApp.Core.Native;

namespace HwMacroApp.Core.Macros;

/// <summary>SendInput API でキーストロークを送信する。</summary>
public sealed class KeyStrokeAction : IMacroAction
{
    public required ushort[] Keys { get; init; }
    public ushort[] Modifiers { get; init; } = [];

    public string DisplayName =>
        string.Join("+", Modifiers.Concat(Keys).Select(k => $"0x{k:X2}"));

    public Task ExecuteAsync(CancellationToken ct = default)
    {
        var allKeys = Modifiers.Concat(Keys).ToArray();
        var inputs = new List<InputStruct>();

        // Key down (modifiers first, then keys)
        foreach (var vk in allKeys)
            inputs.Add(CreateKeyInput(vk, keyUp: false));

        // Key up (reverse order)
        foreach (var vk in allKeys.Reverse())
            inputs.Add(CreateKeyInput(vk, keyUp: true));

        var arr = inputs.ToArray();
        NativeMethods.SendInput((uint)arr.Length, arr, Marshal.SizeOf<InputStruct>());

        return Task.CompletedTask;
    }

    private static InputStruct CreateKeyInput(ushort vk, bool keyUp)
    {
        return new InputStruct
        {
            Type = NativeMethods.INPUT_KEYBOARD,
            Union = new InputUnion
            {
                Keyboard = new KeyboardInput
                {
                    VirtualKey = vk,
                    Flags = keyUp ? NativeMethods.KEYEVENTF_KEYUP : 0,
                }
            }
        };
    }
}
