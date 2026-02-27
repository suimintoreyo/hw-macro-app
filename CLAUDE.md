# CLAUDE.md - プロジェクトメモリ (ルート)

## プロジェクト概要
Windows 向け USB / Bluetooth デバイス常駐マクロソフトウェア (C# / .NET 8 / WPF)

特定の USB または Bluetooth デバイス（テンキー・フットペダル・Bluetooth キーボードなど）を「マクロ専用デバイス」として登録し、そのキー入力を横取りして任意のマクロ（キーストローク送信・アプリ起動・スクリプト実行）を発火させる常駐アプリ。

---

## AI アシスタント向けクイックスタート

### ビルド・テストコマンド
```bash
# コアライブラリのビルド
dotnet build src/HwMacroApp.Core/HwMacroApp.Core.csproj

# WPF アプリのビルド
dotnet build src/HwMacroApp.App/HwMacroApp.App.csproj

# テスト実行 (Windows のみ)
dotnet test tests/HwMacroApp.Core.Tests/

# ソリューション全体ビルド
dotnet build HwMacroApp.sln
```

> **注意**: Raw Input API / LL Hook は Windows 専用。ターゲットは `net8.0-windows`。

### コードを読む順番
1. `src/HwMacroApp.Core/Native/` — P/Invoke 定義 (Win32 API)
2. `src/HwMacroApp.Core/Devices/` — デバイス列挙・入力フック
3. `src/HwMacroApp.Core/Macros/` — マクロアクション定義・実行
4. `src/HwMacroApp.Core/Config/` — 設定モデル・JSON 永続化
5. `src/HwMacroApp.App/` — WPF UI (タスクトレイ + 設定画面)

---

## ブランチ戦略
- `master` — ベースブランチ
- `claude/claude-md-mm48n7m8q9yzkfap-RkUIp` — 現在の開発ブランチ (push 先)
- push 制約: `claude/` プレフィックス + セッション ID 末尾が必要

## リモート
- origin: `suimintoreyo/hw-macro-app`

---

## 技術スタック

| 項目 | 技術 |
|------|------|
| 言語 | C# / .NET 8+ |
| UI | WPF (設定画面) / `System.Windows.Forms.NotifyIcon` (タスクトレイ) |
| 入力取得 | Raw Input API (`RegisterRawInputDevices`) |
| 入力抑制 | Low-Level Keyboard Hook (`WH_KEYBOARD_LL`) |
| デバイス識別 | SetupAPI / デバイスパス解析 (`BTHENUM`/`BTHLE` パターン) |
| マクロ実行 | `SendInput` API / `Process.Start` |
| 設定保存 | JSON (`System.Text.Json`) — `%LOCALAPPDATA%\HwMacroApp\config.json` |
| P/Invoke | `LibraryImport` (source generator、.NET 7+ 推奨方式) |
| テスト | xUnit 2.x |

---

## ソリューション構成

```
HwMacroApp.sln
├── src/HwMacroApp.Core/          # クラスライブラリ (UI 非依存)
│   ├── Devices/                  # デバイス入力層
│   │   ├── RawInputHook.cs       # WM_INPUT 受信・キーイベント発行
│   │   ├── DeviceEnumerator.cs   # 接続中 HID キーボードの列挙
│   │   ├── DeviceFilter.cs       # LL Hook による入力抑制
│   │   ├── DeviceConnectionType.cs  # USB/Bluetooth/BLE enum
│   │   ├── InputDeviceInfo.cs    # デバイス情報 record
│   │   └── RawKeyEvent.cs        # キーイベント record
│   ├── Macros/                   # マクロ実行層
│   │   ├── IMacroAction.cs       # アクション共通インターフェース
│   │   ├── KeyStrokeAction.cs    # SendInput でキーストローク送信
│   │   ├── LaunchAppAction.cs    # Process.Start でアプリ起動 (UseShellExecute=true)
│   │   ├── ScriptAction.cs       # PowerShell スクリプト実行 (UseShellExecute=false)
│   │   ├── DelayAction.cs        # Task.Delay で遅延
│   │   ├── MacroSequence.cs      # 複数アクションの順次実行
│   │   ├── MacroDispatcher.cs    # キーイベント → マクロ実行マッピング
│   │   └── MacroActionFactory.cs # ActionConfig → IMacroAction 生成
│   ├── Config/                   # 設定管理層
│   │   ├── AppConfig.cs          # ルート設定モデル
│   │   ├── DeviceConfig.cs       # デバイス設定 (JsonStringEnumConverter 使用)
│   │   ├── KeyBinding.cs         # キーバインド定義
│   │   ├── ActionConfig.cs       # マクロアクション設定
│   │   ├── GeneralConfig.cs      # 全般設定
│   │   └── ConfigStore.cs        # JSON 読み書き (camelCase + WriteIndented)
│   └── Native/                   # P/Invoke 定義
│       ├── NativeMethods.cs      # Win32 API 宣言 (LibraryImport、internal static partial)
│       └── RawInputStructs.cs    # Win32 構造体定義
├── src/HwMacroApp.App/           # WPF アプリケーション
│   ├── App.xaml(.cs)             # エントリポイント・単一インスタンス制御 (Mutex)
│   ├── Services/TrayIconService.cs  # タスクトレイ管理 (NotifyIcon)
│   ├── ViewModels/MainViewModel.cs  # デバイス設定画面 ViewModel (INotifyPropertyChanged)
│   └── Views/MainWindow.xaml(.cs)  # デバイス一覧・登録画面 (実装済み)
└── tests/HwMacroApp.Core.Tests/  # 単体テスト (xUnit)
    └── (テストファイル未実装)
```

---

## アーキテクチャ概要

```
┌─────────────────────────────────────────────┐
│        HwMacroApp.App (UI Layer)            │
│   Views / ViewModels / TrayIconService      │
│   → CLAUDE.md: src/HwMacroApp.App/          │
├─────────────────────────────────────────────┤
│              HwMacroApp.Core                │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐       │
│  │ Macros  │ │ Config  │ │ Devices │       │
│  │ (実行)  │ │ (設定)  │ │ (入力)  │       │
│  └─────────┘ └─────────┘ └─────────┘       │
│   → CLAUDE.md: 各サブディレクトリ           │
│  ┌─────────────────────────────────────┐   │
│  │           Native (P/Invoke)         │   │
│  └─────────────────────────────────────┘   │
│   → Core/CLAUDE.md に記載                   │
├─────────────────────────────────────────────┤
│              tests/                         │
│   → CLAUDE.md: tests/                       │
└─────────────────────────────────────────────┘
```

## レイヤー間依存関係

```
App ──→ Core (全体)
         │
         ├── Macros ──→ Config (ActionConfig), Native (SendInput)
         ├── Config ──→ (依存なし)
         ├── Devices ──→ Native (P/Invoke)
         └── Native ──→ (依存なし, Win32 API のみ)
```

---

## アプリケーション動作フロー

```
起動
  ↓
Mutex で単一インスタンスチェック ("HwMacroApp_SingleInstance")
  ↓
ConfigStore.Load() → AppConfig (ファイル不在なら空の AppConfig)
  ↓
DeviceFilter に登録済み有効デバイスを Register()
  ↓
MacroDispatcher(config) 初期化
  ↓
RawInputHook.Start() → メッセージウィンドウ作成 + RegisterRawInputDevices
  ↓
DeviceFilter.StartHook() → SetWindowsHookExW(WH_KEYBOARD_LL)
  ↓
TrayIconService.Show() → タスクトレイに常駐 (ShutdownMode=OnExplicitShutdown)

キー入力時
  ↓
[Raw Input] RawInputHook → KeyReceived イベント
  ↓
App.OnKeyReceived:
  1. DeviceFilter.SetLastDevice(path)  ← LL Hook 用に直近デバイスを記録
  2. DeviceFilter.IsRegistered(path) == true ならば
  3. MacroDispatcher.HandleKeyEventAsync(evt)
  ↓
[LL Hook] DeviceFilter.HookCallback:
  - _lastRawInputDevicePath が登録済みなら (IntPtr)1 を返して入力を抑制
  - それ以外は CallNextHookEx で次のフックへ
  ↓
マクロ実行 (KeyStrokeAction / LaunchAppAction / ScriptAction / DelayAction)
```

---

## メモリファイル構成

| ファイル | 役割 |
|----------|------|
| `/CLAUDE.md` (本ファイル) | 全体方針、アーキテクチャ、更新ルール |
| `/src/HwMacroApp.Core/CLAUDE.md` | Core 層の設計方針、Native 定義詳細 |
| `/src/HwMacroApp.Core/Devices/CLAUDE.md` | デバイス入力層の詳細仕様 |
| `/src/HwMacroApp.Core/Macros/CLAUDE.md` | マクロ実行層の詳細仕様 |
| `/src/HwMacroApp.Core/Config/CLAUDE.md` | 設定管理層の詳細仕様 |
| `/src/HwMacroApp.App/CLAUDE.md` | UI 層の設計方針 |
| `/tests/CLAUDE.md` | テスト方針、カバレッジ目標 |

---

## メモリファイル更新ルール

### 原則
1. **実装変更時は該当メモリファイルを必ず更新**
2. **依存レイヤーに影響する場合は関連ファイルも更新**
3. **設計方針変更はルートから下位へ伝播**

### 変更種別と更新対象

| 変更種別 | 更新対象 |
|----------|----------|
| 新規クラス/機能追加 | 該当レイヤー CLAUDE.md |
| API シグネチャ変更 | 該当レイヤー + 依存レイヤー |
| 設計方針変更 | ルート + 影響を受ける全レイヤー |
| バグ修正 (設計変更なし) | 該当レイヤーの「既知の問題」セクション |
| テスト追加/変更 | tests/CLAUDE.md + 該当レイヤー |

### 更新フロー

```
1. 実装変更
     ↓
2. 該当レイヤーの CLAUDE.md 更新
     ↓
3. 依存関係チェック (上記の依存図参照)
     ↓
4. 影響を受けるレイヤーの CLAUDE.md 更新
     ↓
5. テスト対象の場合 tests/CLAUDE.md 更新
     ↓
6. 設計方針変更の場合 ルート CLAUDE.md 更新
```

### 一貫性チェックリスト
- [ ] 新規 public API は該当 CLAUDE.md の「公開 API」セクションに記載したか
- [ ] 依存レイヤーの CLAUDE.md に影響がないか確認したか
- [ ] テスト方針に変更が必要か確認したか

---

## 現在の実装状態

### 完了 (Phase 1-3)
- [x] Phase 1: 基盤構築 (ソリューション、P/Invoke `LibraryImport` 定義)
- [x] Phase 2: コアエンジン (RawInputHook, DeviceFilter, MacroDispatcher, ConfigStore, 全アクションクラス)
- [x] Phase 3: UI 骨格 (タスクトレイ常駐、デバイス一覧・登録画面)

### 未実装 (Phase 4-8)
- [ ] Phase 4: キーバインド設定画面 (`KeyBindingView` / `KeyBindingViewModel`)
- [ ] Phase 5: マクロエディタ画面 (`MacroEditView` / `MacroEditViewModel`)
- [ ] Phase 6: Windows スタートアップ登録 (レジストリ書き込み、`GeneralConfig.StartWithWindows` は保存のみ)
- [ ] Phase 7: テスト実装 (xUnit 単体テスト — テストファイル未作成)
- [ ] Phase 8: インストーラ作成 (MSIX or Inno Setup)

### 実装済みだが未完成の機能
- `WM_DEVICECHANGE` によるデバイス着脱検知 (未実装、手動更新のみ)
- 設定変更後のホットリロード (未対応、アプリ再起動が必要)
- カスタムアイコン (仮: `SystemIcons.Application` 使用中)

---

## キー設計判断 (設計変更時は全レイヤーへ通知)

| 判断事項 | 決定内容 | 理由 |
|----------|----------|------|
| 入力抑制方式 | Raw Input + LL Hook 併用 | ドライバ不要、管理者権限で動作 |
| Bluetooth 判定 | デバイスパスの `BTHENUM`/`BTHLE` パターン | 追加 API 不要で判定可能 |
| 設定形式 | JSON (`System.Text.Json`) | 人間が読める、.NET 標準サポート |
| UI フレームワーク | WPF + `NotifyIcon` | Windows ネイティブ、タスクトレイ対応 |
| P/Invoke スタイル | `LibraryImport` (source generator) | `DllImport` より高性能、.NET 7+ 推奨 |
| 設定ファイルパス | `%LOCALAPPDATA%\HwMacroApp\config.json` | ユーザー固有、管理者権限不要 |
| LL Hook タイミング | WM_INPUT 先処理 → `SetLastDevice` → LL Hook | Raw Input でデバイス特定後に LL Hook で抑制 |
| DeviceConnectionType JSON | `JsonStringEnumConverter` | 文字列 (`"Usb"`, `"Bluetooth"`) で可読性確保 |
