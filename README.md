# HW Macro App - USB / Bluetooth デバイス常駐マクロソフトウェア

Windows上で接続済みの USB・Bluetooth デバイス（キーボード・テンキー・フットペダル・Bluetooth テンキー等）のキー入力をフックし、任意のマクロを実行する常駐アプリケーション。

---

## 概要

特定の USB または Bluetooth デバイスを「マクロ専用デバイス」として登録し、そのデバイスのキー入力を横取りして、事前に設定したマクロ（キーストローク送信・アプリ起動・スクリプト実行など）を発火させる。通常のキーボードには一切影響しない。

### 対応デバイス

| 接続方式 | 例 |
|---------|-----|
| USB (有線) | テンキー、フットペダル、プログラマブルキーボード |
| USB ワイヤレス (2.4GHz ドングル) | ワイヤレステンキー、ワイヤレスミニキーボード |
| Bluetooth / BLE | Bluetooth テンキー、Bluetooth ミニキーボード |

> USB ドングル接続のワイヤレスデバイスは OS から USB HID として認識されるため、USB デバイスと同じ方式で処理される。Bluetooth デバイスは HID over GATT (BLE) または Bluetooth HID Profile 経由で接続され、Raw Input API 上では同様に HID デバイスとして扱える。

## 技術スタック

| 項目 | 技術 |
|------|------|
| 言語 | C# (.NET 8+) |
| UI | WPF (設定画面) / NotifyIcon (タスクトレイ常駐) |
| 入力フック | Raw Input API (`RegisterRawInputDevices`) |
| デバイス識別 | SetupAPI / WMI (`Win32_PnPEntity`) / BluetoothLE APIs |
| マクロ実行 | `SendInput` API / `Process.Start` |
| 設定保存 | JSON (`System.Text.Json`) |
| インストーラ | MSIX または Inno Setup |

## アーキテクチャ

```
┌─────────────────────────────────────────────┐
│              タスクトレイ常駐                  │
│  (NotifyIcon + ContextMenu)                 │
├─────────────────────────────────────────────┤
│              WPF 設定画面                     │
│  ┌──────────┐ ┌──────────┐ ┌──────────────┐│
│  │デバイス   │ │キーバインド│ │マクロエディタ ││
│  │選択      │ │設定       │ │              ││
│  └──────────┘ └──────────┘ └──────────────┘│
├─────────────────────────────────────────────┤
│              コアエンジン                     │
│  ┌──────────┐ ┌──────────┐ ┌──────────────┐│
│  │RawInput  │ │デバイス   │ │マクロ        ││
│  │フック    │→│フィルタ   │→│ディスパッチャ ││
│  └──────────┘ └──────────┘ └──────────────┘│
├─────────────────────────────────────────────┤
│              マクロ実行層                     │
│  ┌──────────┐ ┌──────────┐ ┌──────────────┐│
│  │キー送信  │ │アプリ起動 │ │スクリプト実行 ││
│  │SendInput │ │Process   │ │PowerShell等  ││
│  └──────────┘ └──────────┘ └──────────────┘│
└─────────────────────────────────────────────┘
```

## プロジェクト構成

```
HwMacroApp/
├── HwMacroApp.sln
├── src/
│   ├── HwMacroApp.Core/              # コアロジック (クラスライブラリ)
│   │   ├── Devices/
│   │   │   ├── RawInputHook.cs       # Raw Input API ラッパー
│   │   │   ├── DeviceEnumerator.cs   # USB / Bluetooth デバイス列挙
│   │   │   ├── DeviceFilter.cs       # デバイス識別・フィルタリング
│   │   │   ├── DeviceConnectionType.cs # 接続種別 (USB/Bluetooth/BLE)
│   │   ├── Macros/
│   │   │   ├── IMacroAction.cs       # マクロアクション インターフェース
│   │   │   ├── KeyStrokeAction.cs    # キーストローク送信
│   │   │   ├── LaunchAppAction.cs    # アプリケーション起動
│   │   │   ├── ScriptAction.cs       # スクリプト実行
│   │   │   ├── DelayAction.cs        # 遅延
│   │   │   ├── MacroSequence.cs      # 複数アクションの連結
│   │   │   └── MacroDispatcher.cs    # キー→マクロ マッピング・実行
│   │   ├── Config/
│   │   │   ├── AppConfig.cs          # 設定モデル
│   │   │   ├── KeyBinding.cs         # キーバインド定義
│   │   │   └── ConfigStore.cs        # JSON 読み書き
│   │   └── Native/
│   │       ├── NativeMethods.cs      # P/Invoke 定義
│   │       └── RawInputStructs.cs    # Win32 構造体定義
│   └── HwMacroApp.App/               # WPF アプリケーション
│       ├── App.xaml(.cs)             # エントリポイント・単一起動制御
│       ├── Views/
│       │   ├── MainWindow.xaml       # 設定メイン画面
│       │   ├── DeviceSelectView.xaml # デバイス選択
│       │   ├── KeyBindingView.xaml   # キーバインド設定
│       │   └── MacroEditView.xaml    # マクロ編集
│       ├── ViewModels/
│       │   ├── MainViewModel.cs
│       │   ├── DeviceSelectViewModel.cs
│       │   ├── KeyBindingViewModel.cs
│       │   └── MacroEditViewModel.cs
│       ├── Services/
│       │   └── TrayIconService.cs    # タスクトレイ管理
│       └── Resources/
│           └── tray.ico
└── tests/
    └── HwMacroApp.Core.Tests/
        ├── MacroDispatcherTests.cs
        └── ConfigStoreTests.cs
```

## 主要コンポーネントの設計

### 1. Raw Input によるデバイス別キー入力取得

```csharp
// RawInputHook.cs - 概要
public class RawInputHook : IDisposable
{
    // WM_INPUT メッセージを処理するメッセージ専用ウィンドウ
    // RegisterRawInputDevices で HID キーボードを登録
    // RAWINPUT 構造体からデバイスハンドル + キーコードを取得
    public event Action<RawKeyEvent>? KeyReceived;
}

public record RawKeyEvent(
    IntPtr DeviceHandle,
    string DevicePath,    // \\?\HID#VID_xxxx&PID_xxxx#...
    ushort VKey,
    ushort ScanCode,
    bool IsKeyUp
);
```

### 2. デバイス識別

```csharp
// DeviceEnumerator.cs - 概要
public class DeviceEnumerator
{
    // SetupAPI で USB HID デバイスを列挙
    // WMI / BluetoothLE APIs で Bluetooth HID デバイスを列挙
    // VID/PID/デバイスパスで一意に識別
    public IReadOnlyList<InputDeviceInfo> GetConnectedKeyboards();
}

public enum DeviceConnectionType
{
    Usb,            // USB 有線
    UsbWireless,    // USB ドングル経由 (2.4GHz)
    Bluetooth,      // Bluetooth Classic HID
    BluetoothLE     // Bluetooth Low Energy (HID over GATT)
}

public record InputDeviceInfo(
    string DevicePath,
    string FriendlyName,
    ushort VendorId,
    ushort ProductId,
    DeviceConnectionType ConnectionType
);
```

> **Bluetooth デバイスの識別**: デバイスパスに `BTHENUM` (Bluetooth Classic) や `BTHLE` (BLE) が含まれるかで接続種別を判定する。Raw Input API の `GetRawInputDeviceInfo` で取得できるデバイスパスは接続方式に関わらず HID デバイスとして統一的に扱える。

### 3. マクロアクション

```csharp
public interface IMacroAction
{
    string DisplayName { get; }
    Task ExecuteAsync(CancellationToken ct);
}

// 例: キーストローク送信
public class KeyStrokeAction : IMacroAction
{
    public Keys[] Keys { get; init; }
    public Keys[] Modifiers { get; init; }  // Ctrl, Shift, Alt
    public Task ExecuteAsync(CancellationToken ct) { /* SendInput */ }
}
```

### 4. 設定ファイル形式 (JSON)

```json
{
  "devices": [
    {
      "devicePath": "\\\\?\\HID#VID_1234&PID_5678#...",
      "friendlyName": "USB テンキー",
      "connectionType": "Usb",
      "enabled": true,
      "bindings": [
        {
          "key": "Numpad1",
          "actions": [
            { "type": "keystroke", "keys": ["LControlKey", "C"] },
            { "type": "delay", "ms": 100 },
            { "type": "keystroke", "keys": ["LControlKey", "V"] }
          ]
        },
        {
          "key": "Numpad2",
          "actions": [
            { "type": "launchApp", "path": "notepad.exe", "args": "" }
          ]
        }
      ]
    }
    },
    {
      "devicePath": "\\\\?\\HID#BTHENUM#...",
      "friendlyName": "Bluetooth テンキー",
      "connectionType": "Bluetooth",
      "enabled": true,
      "bindings": [
        {
          "key": "Numpad3",
          "actions": [
            { "type": "keystroke", "keys": ["LControlKey", "Z"] }
          ]
        }
      ]
    }
  ],
  "general": {
    "startWithWindows": true,
    "showNotificationOnMacro": false
  }
}
```

## 実装フェーズ

### Phase 1: 基盤構築
- [ ] ソリューション・プロジェクト作成
- [ ] P/Invoke 定義 (`NativeMethods`, `RawInputStructs`)
- [ ] `RawInputHook` 実装 — メッセージ専用ウィンドウで `WM_INPUT` を受信
- [ ] `DeviceEnumerator` 実装 — 接続中の USB / Bluetooth キーボードデバイスの列挙
- [ ] Bluetooth デバイスパス判定ロジック (`BTHENUM` / `BTHLE` パターン)

### Phase 2: コアエンジン
- [ ] `DeviceFilter` — 登録デバイスからの入力のみ通過、元の入力は抑制
- [ ] `IMacroAction` 各種実装 (`KeyStrokeAction`, `LaunchAppAction`, `DelayAction`)
- [ ] `MacroDispatcher` — キーイベント → マクロ実行のマッピング
- [ ] `ConfigStore` — JSON 設定の読み書き

### Phase 3: UI
- [ ] タスクトレイ常駐 (`NotifyIcon`)
- [ ] デバイス選択画面
- [ ] キーバインド設定画面（キー押下で入力検知）
- [ ] マクロ編集画面（アクションをリストで組み立て）

### Phase 4: 仕上げ
- [ ] Windows スタートアップ登録
- [ ] 単一インスタンス制御 (`Mutex`)
- [ ] エラーハンドリング・ログ出力
- [ ] インストーラ作成

## キー入力抑制の仕組み

Raw Input API 単体ではキー入力を抑制できないため、以下のいずれかを併用する:

1. **Low-Level Keyboard Hook (`SetWindowsHookEx` + `WH_KEYBOARD_LL`)**
   - `WndProc` で `WM_INPUT` を受け取りデバイスを判定
   - 対象デバイスからの入力なら、LL Hook 側で `CallNextHookEx` を呼ばず入力を消す
2. **Interception Driver (代替案)**
   - [Interception](https://github.com/oblitum/Interception) ドライバを利用
   - カーネルレベルで特定デバイスの入力を横取り可能
   - ドライバ署名の制約に注意

推奨: まず方式1で実装し、抑制精度に問題があれば方式2を検討。

## Bluetooth デバイス固有の考慮事項

| 課題 | 対応 |
|------|------|
| 接続・切断の動的検知 | `WM_DEVICECHANGE` メッセージを監視し、デバイスの着脱をリアルタイム検知 |
| スリープ復帰時の再接続 | デバイスパスが変わる可能性があるため、VID/PID + デバイス名で再マッチング |
| BLE の遅延 | BLE デバイスは接続確立に数秒かかる場合がある。接続状態を UI に表示 |
| ペアリング管理 | 本アプリではペアリング自体は OS 側で行う前提。列挙はペアリング済みデバイスのみ |

## ビルド・実行

```bash
# ビルド
dotnet build src/HwMacroApp.App/HwMacroApp.App.csproj

# 実行 (管理者権限推奨)
dotnet run --project src/HwMacroApp.App

# テスト
dotnet test tests/HwMacroApp.Core.Tests
```

> **注意**: Raw Input / LL Hook の利用には管理者権限が必要な場合があります。

## ライセンス

MIT
