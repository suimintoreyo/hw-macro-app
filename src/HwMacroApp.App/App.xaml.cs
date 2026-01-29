using System.Threading;
using System.Windows;
using HwMacroApp.App.Services;
using HwMacroApp.Core.Config;
using HwMacroApp.Core.Devices;
using HwMacroApp.Core.Macros;

namespace HwMacroApp.App;

public partial class App : Application
{
    private Mutex? _mutex;
    private TrayIconService? _trayIcon;
    private RawInputHook? _rawInputHook;
    private DeviceFilter? _deviceFilter;
    private MacroDispatcher? _dispatcher;

    protected override void OnStartup(StartupEventArgs e)
    {
        // 単一インスタンス制御
        _mutex = new Mutex(true, "HwMacroApp_SingleInstance", out bool isNew);
        if (!isNew)
        {
            MessageBox.Show("HW Macro App は既に起動しています。", "HW Macro App",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var configStore = new ConfigStore();
        var config = configStore.Load();

        // デバイスフィルタ
        _deviceFilter = new DeviceFilter();
        foreach (var dev in config.Devices.Where(d => d.Enabled))
            _deviceFilter.Register(dev.DevicePath);

        // マクロディスパッチャ
        _dispatcher = new MacroDispatcher(config);

        // Raw Input フック
        _rawInputHook = new RawInputHook();
        _rawInputHook.KeyReceived += OnKeyReceived;
        _rawInputHook.Start();

        // LL Keyboard Hook
        _deviceFilter.StartHook();

        // タスクトレイ
        _trayIcon = new TrayIconService();
        _trayIcon.ShowSettingsRequested += OnShowSettings;
        _trayIcon.ExitRequested += OnExit;
        _trayIcon.Show();
    }

    private async void OnKeyReceived(RawKeyEvent evt)
    {
        _deviceFilter?.SetLastDevice(evt.DevicePath);

        if (_deviceFilter?.IsRegistered(evt.DevicePath) == true && _dispatcher != null)
        {
            await _dispatcher.HandleKeyEventAsync(evt);
        }
    }

    private void OnShowSettings()
    {
        var existing = Windows.OfType<Views.MainWindow>().FirstOrDefault();
        if (existing != null)
        {
            existing.Activate();
            return;
        }

        var window = new Views.MainWindow();
        window.Show();
    }

    private void OnExit()
    {
        _rawInputHook?.Dispose();
        _deviceFilter?.Dispose();
        _trayIcon?.Dispose();
        _mutex?.Dispose();
        Shutdown();
    }
}
