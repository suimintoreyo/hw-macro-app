using System.Runtime.InteropServices;

namespace HwMacroApp.Core.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct RawInputDevice
{
    public ushort UsagePage;
    public ushort Usage;
    public uint Flags;
    public IntPtr Target;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawInputDeviceList
{
    public IntPtr Device;
    public uint Type;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawInputHeader
{
    public uint Type;
    public uint Size;
    public IntPtr Device;
    public IntPtr WParam;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawKeyboard
{
    public ushort MakeCode;
    public ushort Flags;
    public ushort Reserved;
    public ushort VKey;
    public uint Message;
    public uint ExtraInformation;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawInput
{
    public RawInputHeader Header;
    public RawKeyboard Keyboard;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WndClassW
{
    public uint Style;
    public NativeMethods.WndProc WndProc;
    public int CbClsExtra;
    public int CbWndExtra;
    public IntPtr Instance;
    public IntPtr Icon;
    public IntPtr Cursor;
    public IntPtr Background;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string? MenuName;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string ClassName;
}

// ── SendInput 構造体 ──

[StructLayout(LayoutKind.Sequential)]
internal struct InputStruct
{
    public uint Type;
    public InputUnion Union;
}

[StructLayout(LayoutKind.Explicit)]
internal struct InputUnion
{
    [FieldOffset(0)]
    public KeyboardInput Keyboard;
}

[StructLayout(LayoutKind.Sequential)]
internal struct KeyboardInput
{
    public ushort VirtualKey;
    public ushort ScanCode;
    public uint Flags;
    public uint Time;
    public IntPtr ExtraInfo;
}

// ── Low-Level Keyboard Hook 構造体 ──

[StructLayout(LayoutKind.Sequential)]
internal struct KbDllHookStruct
{
    public uint VkCode;
    public uint ScanCode;
    public uint Flags;
    public uint Time;
    public IntPtr ExtraInfo;
}
