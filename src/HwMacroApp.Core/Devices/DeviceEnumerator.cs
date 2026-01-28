using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using HwMacroApp.Core.Native;

namespace HwMacroApp.Core.Devices;

/// <summary>
/// 接続中の HID キーボードデバイス (USB / Bluetooth) を列挙する。
/// </summary>
public sealed partial class DeviceEnumerator
{
    [GeneratedRegex(@"VID_([0-9A-Fa-f]{4})")]
    private static partial Regex VidRegex();

    [GeneratedRegex(@"PID_([0-9A-Fa-f]{4})")]
    private static partial Regex PidRegex();

    /// <summary>接続中のキーボードデバイス一覧を返す。</summary>
    public IReadOnlyList<InputDeviceInfo> GetConnectedKeyboards()
    {
        uint deviceCount = 0;
        var listSize = (uint)Marshal.SizeOf<RawInputDeviceList>();
        NativeMethods.GetRawInputDeviceList(null, ref deviceCount, listSize);

        if (deviceCount == 0) return [];

        var deviceList = new RawInputDeviceList[deviceCount];
        NativeMethods.GetRawInputDeviceList(deviceList, ref deviceCount, listSize);

        var results = new List<InputDeviceInfo>();

        foreach (var dev in deviceList)
        {
            // キーボードデバイスのみ
            if (dev.Type != NativeMethods.RIM_TYPEKEYBOARD) continue;

            var path = GetDeviceName(dev.Device);
            if (string.IsNullOrEmpty(path)) continue;

            var vid = ParseHex(VidRegex().Match(path));
            var pid = ParseHex(PidRegex().Match(path));
            var connType = DetermineConnectionType(path);
            var friendlyName = BuildFriendlyName(path, vid, pid, connType);

            results.Add(new InputDeviceInfo(path, friendlyName, vid, pid, connType));
        }

        return results;
    }

    /// <summary>デバイスパスから接続種別を判定する。</summary>
    internal static DeviceConnectionType DetermineConnectionType(string devicePath)
    {
        var upper = devicePath.ToUpperInvariant();

        if (upper.Contains("BTHLE"))
            return DeviceConnectionType.BluetoothLE;

        if (upper.Contains("BTHENUM"))
            return DeviceConnectionType.Bluetooth;

        // USB デバイスのうちワイヤレスドングルかどうかはパスだけでは判別困難。
        // ここでは USB として統一し、将来的に WMI 等で細分化可能。
        return DeviceConnectionType.Usb;
    }

    private static string GetDeviceName(IntPtr deviceHandle)
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

    private static ushort ParseHex(Match match)
    {
        if (!match.Success) return 0;
        return Convert.ToUInt16(match.Groups[1].Value, 16);
    }

    private static string BuildFriendlyName(string path, ushort vid, ushort pid, DeviceConnectionType connType)
    {
        var prefix = connType switch
        {
            DeviceConnectionType.Bluetooth => "BT",
            DeviceConnectionType.BluetoothLE => "BLE",
            _ => "USB",
        };
        return $"{prefix} Keyboard ({vid:X4}:{pid:X4})";
    }
}
