using System.Drawing;
using System.Windows.Forms;

namespace HwMacroApp.App.Services;

/// <summary>タスクトレイ常駐アイコンを管理する。</summary>
public sealed class TrayIconService : IDisposable
{
    private NotifyIcon? _notifyIcon;

    public event Action? ShowSettingsRequested;
    public event Action? ExitRequested;

    public void Show()
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("設定を開く", null, (_, _) => ShowSettingsRequested?.Invoke());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("終了", null, (_, _) => ExitRequested?.Invoke());

        _notifyIcon = new NotifyIcon
        {
            Text = "HW Macro App",
            Icon = SystemIcons.Application, // TODO: カスタムアイコンに差し替え
            ContextMenuStrip = contextMenu,
            Visible = true,
        };

        _notifyIcon.DoubleClick += (_, _) => ShowSettingsRequested?.Invoke();
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
