using System.Runtime.InteropServices;
using HwMacroApp.Core.Native;

namespace HwMacroApp.Core.Devices;

/// <summary>
/// メッセージ専用ウィンドウで WM_INPUT を受信し、キーボード Raw Input イベントを発行する。
/// </summary>
public sealed class RawInputHook : IDisposable
{
    private IntPtr _hwnd;
    private NativeMethods.WndProc? _wndProcDelegate; // prevent GC
    private bool _disposed;

    /// <summary>デバイス別キーイベント。UI スレッドから発火する。</summary>
    public event Action<RawKeyEvent>? KeyReceived;

    /// <summary>フックを開始する。WPF Dispatcher スレッドから呼ぶこと。</summary>
    public void Start()
    {
        _wndProcDelegate = WndProc;

        var hInstance = NativeMethods.GetModuleHandleW(null);
        var className = "HwMacroApp_RawInput_" + Guid.NewGuid().ToString("N");

        var wc = new WndClassW
        {
            WndProc = _wndProcDelegate,
            Instance = hInstance,
            ClassName = className,
        };

        if (NativeMethods.RegisterClassW(ref wc) == 0)
            throw new InvalidOperationException("RegisterClass failed");

        _hwnd = NativeMethods.CreateWindowExW(
            0, className, "HwMacroApp RawInput", 0,
            0, 0, 0, 0,
            NativeMethods.HWND_MESSAGE, IntPtr.Zero, hInstance, IntPtr.Zero);

        if (_hwnd == IntPtr.Zero)
            throw new InvalidOperationException("CreateWindowEx failed");

        RegisterKeyboardRawInput();
    }

    private void RegisterKeyboardRawInput()
    {
        var rid = new RawInputDevice[]
        {
            new()
            {
                UsagePage = NativeMethods.HID_USAGE_PAGE_GENERIC,
                Usage = NativeMethods.HID_USAGE_GENERIC_KEYBOARD,
                Flags = NativeMethods.RIDEV_INPUTSINK,
                Target = _hwnd,
            }
        };

        if (!NativeMethods.RegisterRawInputDevices(rid, (uint)rid.Length,
                (uint)Marshal.SizeOf<RawInputDevice>()))
        {
            throw new InvalidOperationException("RegisterRawInputDevices failed");
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == NativeMethods.WM_INPUT)
        {
            ProcessRawInput(lParam);
            return IntPtr.Zero;
        }
        return NativeMethods.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private void ProcessRawInput(IntPtr lParam)
    {
        uint size = 0;
        var headerSize = (uint)Marshal.SizeOf<RawInputHeader>();

        NativeMethods.GetRawInputData(lParam, NativeMethods.RID_INPUT,
            IntPtr.Zero, ref size, headerSize);

        if (size == 0) return;

        var buffer = Marshal.AllocHGlobal((int)size);
        try
        {
            if (NativeMethods.GetRawInputData(lParam, NativeMethods.RID_INPUT,
                    buffer, ref size, headerSize) == unchecked((uint)-1))
                return;

            var raw = Marshal.PtrToStructure<RawInput>(buffer);
            if (raw.Header.Type != NativeMethods.RIM_TYPEKEYBOARD) return;

            var devicePath = GetDevicePath(raw.Header.Device);
            bool isKeyUp = (raw.Keyboard.Flags & 0x01) != 0;

            var evt = new RawKeyEvent(
                raw.Header.Device,
                devicePath,
                raw.Keyboard.VKey,
                raw.Keyboard.MakeCode,
                isKeyUp);

            KeyReceived?.Invoke(evt);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string GetDevicePath(IntPtr deviceHandle)
    {
        uint size = 0;
        NativeMethods.GetRawInputDeviceInfoW(deviceHandle,
            NativeMethods.RIDI_DEVICENAME, IntPtr.Zero, ref size);

        if (size == 0) return string.Empty;

        var buffer = Marshal.AllocHGlobal((int)(size * 2));
        try
        {
            NativeMethods.GetRawInputDeviceInfoW(deviceHandle,
                NativeMethods.RIDI_DEVICENAME, buffer, ref size);
            return Marshal.PtrToStringUni(buffer) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_hwnd != IntPtr.Zero)
        {
            NativeMethods.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }
}
