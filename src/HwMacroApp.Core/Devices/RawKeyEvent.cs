namespace HwMacroApp.Core.Devices;

/// <summary>
/// Raw Input API から取得したキーイベント。
/// </summary>
public sealed record RawKeyEvent(
    IntPtr DeviceHandle,
    string DevicePath,
    ushort VKey,
    ushort ScanCode,
    bool IsKeyUp);
