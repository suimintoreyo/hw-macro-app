namespace HwMacroApp.Core.Devices;

public sealed record InputDeviceInfo(
    string DevicePath,
    string FriendlyName,
    ushort VendorId,
    ushort ProductId,
    DeviceConnectionType ConnectionType);
