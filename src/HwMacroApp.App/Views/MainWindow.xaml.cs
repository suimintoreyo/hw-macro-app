using System.Windows;
using HwMacroApp.App.ViewModels;
using HwMacroApp.Core.Config;
using HwMacroApp.Core.Devices;

namespace HwMacroApp.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e) =>
        _vm.RefreshDevices();

    private void OnRegisterClick(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedDevice is { } device)
            _vm.RegisterDevice(device);
    }

    private void OnUnregisterClick(object sender, RoutedEventArgs e)
    {
        if (RegisteredDevicesList.SelectedItem is DeviceConfig device)
            _vm.UnregisterDevice(device);
    }
}
