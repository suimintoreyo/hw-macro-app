# CLAUDE.md - HwMacroApp.App (UI Layer)

## 概要
WPF ベースの設定画面とタスクトレイ常駐機能を提供。`ShutdownMode="OnExplicitShutdown"` でウィンドウを閉じてもアプリが終了しない。

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
| (root) | `App.xaml` | アプリケーション定義 (`ShutdownMode=OnExplicitShutdown`) |
| (root) | `App.xaml.cs` | エントリポイント、初期化、シャットダウン |
| Services | `TrayIconService.cs` | タスクトレイアイコン管理 |
| ViewModels | `MainViewModel.cs` | メイン画面の ViewModel |
| Views | `MainWindow.xaml/.cs` | デバイス一覧・登録画面 (実装済み) |
| Resources | (`.gitkeep_resources`) | リソースディレクトリ予約 (カスタムアイコン未設定) |

---

## アプリケーションライフサイクル

### 起動シーケンス (`App.OnStartup`)
```
1. Mutex("HwMacroApp_SingleInstance") で単一インスタンスチェック
   → 既に起動中なら MessageBox 表示 + Shutdown()
2. ConfigStore().Load() で設定読み込み
3. DeviceFilter 初期化 + 有効デバイスを Register()
4. MacroDispatcher(config) 初期化
5. RawInputHook 初期化 + KeyReceived イベント接続
6. RawInputHook.Start() → WM_INPUT 受信開始
7. DeviceFilter.StartHook() → LL Hook 開始
8. TrayIconService 初期化 + イベント接続 + Show()
```

### キー入力ハンドラ (`App.OnKeyReceived`)
```
1. DeviceFilter.SetLastDevice(evt.DevicePath)  ← LL Hook 用に記録 (必須・先頭)
2. DeviceFilter.IsRegistered(evt.DevicePath) == true ならば
3. MacroDispatcher.HandleKeyEventAsync(evt)  ← async void で fire-and-forget
```

### 終了シーケンス (`App.OnExit` via `TrayIconService.ExitRequested`)
```
1. RawInputHook.Dispose()
2. DeviceFilter.Dispose()
3. TrayIconService.Dispose()
4. Mutex.Dispose()
5. Application.Shutdown()
```

---

## 公開 API / 主要クラス

### App.xaml.cs

**保持するインスタンス** (フィールド):
- `Mutex? _mutex` — 単一インスタンス制御
- `TrayIconService? _trayIcon` — タスクトレイ
- `RawInputHook? _rawInputHook` — 入力フック
- `DeviceFilter? _deviceFilter` — 入力抑制
- `MacroDispatcher? _dispatcher` — マクロ実行

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

**実装詳細**:
- `System.Windows.Forms.NotifyIcon` を使用 (WPF 標準になし)
- アイコン: `SystemIcons.Application` (仮、カスタムアイコン未設定)
- コンテキストメニュー:
  - 「設定を開く」→ `ShowSettingsRequested`
  - 「終了」→ `ExitRequested`
- ダブルクリック → `ShowSettingsRequested`

### MainViewModel

```csharp
public sealed class MainViewModel : INotifyPropertyChanged
{
    public ObservableCollection<InputDeviceInfo> AvailableDevices { get; }
    public ObservableCollection<DeviceConfig> RegisteredDevices { get; }
    public InputDeviceInfo? SelectedDevice { get; set; }

    public void RefreshDevices();        // 接続デバイス再取得 + 登録済みリスト更新
    public void RegisterDevice(InputDeviceInfo device);   // 登録 + 即時 Save()
    public void UnregisterDevice(DeviceConfig device);    // 解除 + 即時 Save()
    public void Save();                  // ConfigStore.Save(config)
}
```

**注意**: コンストラクタで `ConfigStore().Load()` と `RefreshDevices()` を実行。

---

## MainWindow 画面構成 (実装済み)

```
┌─────────────────────────────────────┐
│ 接続中デバイス           [更新]    │
├─────────────────────────────────────┤
│ デバイス名  | 接続  | VID  | PID   │  ← AvailableDevices (ListView)
│ ─────────────────────────────────  │
│ USB Kbd    | Usb   | 1234 | 5678  │
│ BT Numpad  | BT    | ABCD | EF01  │
├─────────────────────────────────────┤
│ 登録済みデバイス  [登録] [解除]   │
├─────────────────────────────────────┤
│ デバイス名  | 接続  | 有効 | バインド数 │  ← RegisteredDevices (ListView)
│ ─────────────────────────────────  │
│ USB Kbd    | Usb   | True | 3     │
└─────────────────────────────────────┘
```

### コードビハインド (MainWindow.xaml.cs)
```csharp
// ボタンクリックは DataContext (MainViewModel) を直接呼ぶ簡易実装
private void OnRefreshClick(...)    → _vm.RefreshDevices()
private void OnRegisterClick(...)   → _vm.RegisterDevice(_vm.SelectedDevice)
private void OnUnregisterClick(...) → _vm.UnregisterDevice(...)
```

---

## MVVM パターン

### 現在の実装
- **View**: XAML + コードビハインド (簡易実装、ICommand 未使用)
- **ViewModel**: `INotifyPropertyChanged` 実装 (`CallerMemberName` 属性使用)
- **Model**: Core 層のクラス (`InputDeviceInfo`, `DeviceConfig`, etc.)

### 将来的な改善
- `ICommand` 実装でコマンドバインディング
- または `CommunityToolkit.Mvvm` の導入

---

## 画面構成

### 実装済み
- [x] `MainWindow` — デバイス一覧・登録画面 (600x800, CenterScreen)

### 未実装
- [ ] `KeyBindingView` — キーバインド設定画面 (Phase 4)
- [ ] `MacroEditView` — マクロエディタ画面 (Phase 5)
- [ ] `GeneralSettingsView` — 一般設定画面

---

## テスト方針

### 単体テスト可能
- `MainViewModel` — `DeviceEnumerator` と `ConfigStore` をモック化 (要インターフェース抽出)

### 推奨モックインターフェース
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
- タスクトレイ動作 (表示・コンテキストメニュー)
- ウィンドウ表示/非表示・重複開き防止
- UI 操作全般 (登録・解除・更新)

---

## 将来の拡張
- [ ] キーバインド設定画面 (キー押下で VKey 入力検知)
- [ ] マクロエディタ (ドラッグ&ドロップでアクション追加)
- [ ] Windows スタートアップ登録 UI (Phase 6)
- [ ] 通知バルーン (マクロ実行時、`ShowNotificationOnMacro`)
- [ ] カスタムアイコン (`tray.ico`) の設定
- [ ] ダークモード対応
- [ ] 多言語対応

---

## 既知の問題
- `TrayIconService` は `System.Windows.Forms.NotifyIcon` を使用 (WPF 標準になし)
- アイコンは `SystemIcons.Application` を仮使用中 (カスタムアイコン未設定)
- 設定変更後のホットリロード未対応 (アプリ再起動が必要)
- `MainViewModel` はコンストラクタで直接 `ConfigStore` を new するためテストしにくい (モック化にはリファクタリングが必要)
- キーバインド設定・マクロエディタ画面は XAML/ViewModel ともに未作成
