using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HwMacroApp.Core.Config;
using HwMacroApp.Core.Devices;

namespace HwMacroApp.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ConfigStore _configStore;
    private readonly DeviceEnumerator _enumerator = new();
    private AppConfig _config;

    public MainViewModel()
    {
        _configStore = new ConfigStore();
        _config = _configStore.Load();
        RefreshDevices();
    }

    public ObservableCollection<InputDeviceInfo> AvailableDevices { get; } = new();
    public ObservableCollection<DeviceConfig> RegisteredDevices { get; } = new();

    private InputDeviceInfo? _selectedDevice;
    public InputDeviceInfo? SelectedDevice
    {
        get => _selectedDevice;
        set { _selectedDevice = value; OnPropertyChanged(); }
    }

    public void RefreshDevices()
    {
        AvailableDevices.Clear();
        foreach (var dev in _enumerator.GetConnectedKeyboards())
            AvailableDevices.Add(dev);

        RegisteredDevices.Clear();
        foreach (var dev in _config.Devices)
            RegisteredDevices.Add(dev);
    }

    public void RegisterDevice(InputDeviceInfo device)
    {
        if (_config.Devices.Any(d => d.DevicePath.Equals(device.DevicePath, StringComparison.OrdinalIgnoreCase)))
            return;

        var deviceConfig = new DeviceConfig
        {
            DevicePath = device.DevicePath,
            FriendlyName = device.FriendlyName,
            ConnectionType = device.ConnectionType,
            Enabled = true,
        };

        _config.Devices.Add(deviceConfig);
        RegisteredDevices.Add(deviceConfig);
        Save();
    }

    public void UnregisterDevice(DeviceConfig device)
    {
        _config.Devices.Remove(device);
        RegisteredDevices.Remove(device);
        Save();
    }

    public void Save()
    {
        _configStore.Save(_config);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
