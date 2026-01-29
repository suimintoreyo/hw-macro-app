# CLAUDE.md - HwMacroApp.App (UI Layer)

## 概要
WPF ベースの設定画面とタスクトレイ常駐機能を提供。

## 依存関係
```
App → HwMacroApp.Core (全体)
    → Devices (RawInputHook, DeviceFilter, DeviceEnumerator)
    → Macros (MacroDispatcher)
    → Config (ConfigStore, AppConfig)
```

---

## ファイル構成

| ディレクトリ | ファイル | 役割 |
|--------------|----------|------|
| (root) | `App.xaml` | アプリケーション定義 |
| (root) | `App.xaml.cs` | エントリポイント、初期化、シャットダウン |
| Services | `TrayIconService.cs` | タスクトレイアイコン管理 |
| ViewModels | `MainViewModel.cs` | メイン画面の ViewModel |
| Views | `MainWindow.xaml/.cs` | デバイス設定画面 |
| Resources | (icons) | アイコンリソース |

---

## アプリケーションライフサイクル

### 起動シーケンス
```
1. App.OnStartup()
   ├── Mutex で単一インスタンスチェック
   ├── ConfigStore.Load() で設定読み込み
   ├── DeviceFilter 初期化 + 登録デバイス追加
   ├── MacroDispatcher 初期化
   ├── RawInputHook.Start()
   ├── DeviceFilter.StartHook()
   └── TrayIconService.Show()
```

### 終了シーケンス
```
1. TrayIconService.ExitRequested イベント
   ├── RawInputHook.Dispose()
   ├── DeviceFilter.Dispose()
   ├── TrayIconService.Dispose()
   ├── Mutex.Dispose()
   └── Application.Shutdown()
```

---

## 公開 API / 主要クラス

### App.xaml.cs

```csharp
public partial class App : Application
{
    // ShutdownMode = OnExplicitShutdown
    // → ウィンドウを閉じてもアプリは終了しない
}
```

**保持するインスタンス**:
- `Mutex` — 単一インスタンス制御
- `TrayIconService` — タスクトレイ
- `RawInputHook` — 入力フック
- `DeviceFilter` — 入力抑制
- `MacroDispatcher` — マクロ実行

### TrayIconService

```csharp
public sealed class TrayIconService : IDisposable
{
    public event Action? ShowSettingsRequested;
    public event Action? ExitRequested;
    public void Show();
    public void Dispose();
}
```

**コンテキストメニュー**:
- 「設定を開く」→ `ShowSettingsRequested`
- 「終了」→ `ExitRequested`

**ダブルクリック**: `ShowSettingsRequested`

### MainViewModel

```csharp
public sealed class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<InputDeviceInfo> AvailableDevices { get; }
    public ObservableCollection<DeviceConfig> RegisteredDevices { get; }
    public InputDeviceInfo? SelectedDevice { get; set; }

    public void RefreshDevices();
    public void RegisterDevice(InputDeviceInfo device);
    public void UnregisterDevice(DeviceConfig device);
    public void Save();
}
```

---

## MVVM パターン

### 現在の実装
- **View**: XAML + コードビハインド (簡易実装)
- **ViewModel**: `INotifyPropertyChanged` 実装
- **Model**: Core 層のクラス

### コードビハインドの使用
現在は簡易実装のため、ボタンクリックはコードビハインドで処理:

```csharp
private void OnRefreshClick(object sender, RoutedEventArgs e) =>
    _vm.RefreshDevices();
```

**将来的な改善**:
- `ICommand` 実装でコマンドバインディング
- または CommunityToolkit.Mvvm 導入

---

## 画面構成

### MainWindow (実装済み)
```
┌─────────────────────────────────────┐
│ 接続中デバイス           [更新]    │
├─────────────────────────────────────┤
│ デバイス名 | 接続 | VID  | PID     │
│ ───────────────────────────────────│
│ USB Kbd   | Usb  | 1234 | 5678    │
│ BT Numpad | BT   | ABCD | EF01    │
├─────────────────────────────────────┤
│ 登録済みデバイス    [登録] [解除]  │
├─────────────────────────────────────┤
│ デバイス名 | 接続 | 有効 | バインド│
│ ───────────────────────────────────│
│ USB Kbd   | Usb  | True | 3       │
└─────────────────────────────────────┘
```

### 未実装画面
- [ ] キーバインド設定画面 (`KeyBindingView`)
- [ ] マクロエディタ画面 (`MacroEditView`)
- [ ] 一般設定画面 (`GeneralSettingsView`)

---

## テスト方針

### 単体テスト可能
- `MainViewModel` — `DeviceEnumerator` と `ConfigStore` をモック化

### モック対象
```csharp
public interface IDeviceEnumerator
{
    IReadOnlyList<InputDeviceInfo> GetConnectedKeyboards();
}

public interface IConfigStore
{
    AppConfig Load();
    void Save(AppConfig config);
}
```

### 手動テスト必要
- タスクトレイ動作
- ウィンドウ表示/非表示
- UI 操作全般

---

## 将来の拡張
- [ ] キーバインド設定画面
- [ ] マクロエディタ (ドラッグ&ドロップでアクション追加)
- [ ] Windows スタートアップ登録 UI
- [ ] 通知バルーン (マクロ実行時)
- [ ] ダークモード対応
- [ ] 多言語対応

---

## 既知の問題
- `TrayIconService` は `System.Windows.Forms.NotifyIcon` を使用 (WPF 標準になし)
- アイコンは `SystemIcons.Application` を仮使用中 (カスタムアイコン未設定)
- 設定変更後のホットリロード未対応 (アプリ再起動が必要)
