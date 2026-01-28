using System.Runtime.InteropServices;
using HwMacroApp.Core.Native;

namespace HwMacroApp.Core.Devices;

/// <summary>
/// 登録済みデバイスからの入力のみ通過させ、Low-Level Keyboard Hook で入力を抑制する。
/// </summary>
public sealed class DeviceFilter : IDisposable
{
    private readonly HashSet<string> _registeredPaths = new(StringComparer.OrdinalIgnoreCase);
    private IntPtr _hookHandle;
    private NativeMethods.LowLevelKeyboardProc? _hookProc;
    private volatile string? _lastRawInputDevicePath;
    private bool _disposed;

    /// <summary>抑制対象デバイスパスを登録する。</summary>
    public void Register(string devicePath) => _registeredPaths.Add(devicePath);

    /// <summary>抑制対象デバイスパスを解除する。</summary>
    public void Unregister(string devicePath) => _registeredPaths.Remove(devicePath);

    /// <summary>登録済みデバイスかどうか。</summary>
    public bool IsRegistered(string devicePath) =>
        _registeredPaths.Contains(devicePath);

    /// <summary>
    /// RawInputHook から呼び出し、直近のデバイスパスを記録する。
    /// LL Hook コールバックではデバイス判定ができないため、
    /// WM_INPUT が先に処理されることを利用して直前のデバイスパスを保持する。
    /// </summary>
    public void SetLastDevice(string devicePath) =>
        _lastRawInputDevicePath = devicePath;

    /// <summary>Low-Level Keyboard Hook を開始する。</summary>
    public void StartHook()
    {
        _hookProc = HookCallback;
        var hModule = NativeMethods.GetModuleHandleW(null);
        _hookHandle = NativeMethods.SetWindowsHookExW(
            NativeMethods.WH_KEYBOARD_LL, _hookProc, hModule, 0);

        if (_hookHandle == IntPtr.Zero)
            throw new InvalidOperationException("SetWindowsHookEx failed");
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var lastPath = _lastRawInputDevicePath;
            if (lastPath != null && _registeredPaths.Contains(lastPath))
            {
                // 登録済みデバイスからの入力を抑制（次のフックへ渡さない）
                return (IntPtr)1;
            }
        }
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }
}
