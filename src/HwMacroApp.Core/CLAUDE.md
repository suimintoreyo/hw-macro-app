# CLAUDE.md - HwMacroApp.Core

## 概要
コアロジックを提供するクラスライブラリ。UI 非依存。`net8.0-windows` ターゲット。

## 依存関係
```
このレイヤー → 依存先
━━━━━━━━━━━━━━━━━━━━━━━━━
Macros   → Config (ActionConfig), Native (SendInput)
Devices  → Native (P/Invoke)
Config   → (なし)
Native   → (なし, Win32 API のみ)
```

## サブレイヤー構成

| サブレイヤー | 役割 | 詳細 CLAUDE.md |
|--------------|------|----------------|
| Devices | デバイス列挙、Raw Input、入力抑制 | `Devices/CLAUDE.md` |
| Macros | マクロアクション定義・実行 | `Macros/CLAUDE.md` |
| Config | 設定モデル、JSON 永続化 | `Config/CLAUDE.md` |
| Native | P/Invoke 定義 | 本ファイルに記載 |

---

## Native レイヤー詳細

### ファイル構成
- `Native/NativeMethods.cs` — P/Invoke 関数宣言 (`internal static partial class`)
- `Native/RawInputStructs.cs` — Win32 構造体定義

### P/Invoke スタイル
`LibraryImport` (source generator) を使用。`DllImport` は使用しない。

```csharp
// 正しいパターン
[LibraryImport("user32.dll", SetLastError = true)]
public static partial bool RegisterRawInputDevices(...);

// 文字列引数がある場合
[LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
public static partial IntPtr GetModuleHandleW(string? lpModuleName);
```

### P/Invoke 関数一覧

| カテゴリ | 関数 | 用途 |
|----------|------|------|
| Raw Input | `RegisterRawInputDevices` | HID デバイスの入力受信登録 |
| Raw Input | `GetRawInputData` | WM_INPUT から入力データ取得 |
| Raw Input | `GetRawInputDeviceInfoW` | デバイスパス・情報取得 |
| Raw Input | `GetRawInputDeviceList` | 接続デバイス列挙 |
| LL Hook | `SetWindowsHookExW` | キーボードフック開始 |
| LL Hook | `CallNextHookEx` | 次のフックへ伝播 |
| LL Hook | `UnhookWindowsHookEx` | フック解除 |
| SendInput | `SendInput` | キーストローク送信 |
| Window | `RegisterClassW` | ウィンドウクラス登録 |
| Window | `CreateWindowExW` | メッセージ専用ウィンドウ作成 |
| Window | `DestroyWindow` | ウィンドウ破棄 |
| Window | `DefWindowProcW` | デフォルトウィンドウプロシージャ |
| Module | `GetModuleHandleW` | モジュールハンドル取得 |

### デリゲート定義
```csharp
public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
```

### 定数定義

```csharp
// ── Window Messages ──
WM_INPUT        = 0x00FF
WM_DEVICECHANGE = 0x0219
WM_KEYDOWN      = 0x0100
WM_KEYUP        = 0x0101
WM_SYSKEYDOWN   = 0x0104
WM_SYSKEYUP     = 0x0105

// ── Raw Input ──
RIDEV_INPUTSINK = 0x00000100
RID_INPUT       = 0x10000003
RIM_TYPEKEYBOARD = 1
RIDI_DEVICENAME = 0x20000007
RIDI_DEVICEINFO = 0x2000000b

// ── HID Usage ──
HID_USAGE_PAGE_GENERIC    = 0x01
HID_USAGE_GENERIC_KEYBOARD = 0x06

// ── Hook ──
WH_KEYBOARD_LL = 13

// ── SendInput ──
INPUT_KEYBOARD     = 1
KEYEVENTF_KEYUP    = 0x0002
KEYEVENTF_EXTENDEDKEY = 0x0001

// ── Special Window Handles ──
HWND_MESSAGE = new IntPtr(-3)  // メッセージ専用ウィンドウの親
```

### 構造体一覧

| 構造体 | 用途 |
|--------|------|
| `RawInputDevice` | `RegisterRawInputDevices` 用 |
| `RawInputDeviceList` | `GetRawInputDeviceList` 用 |
| `RawInputHeader` | RAWINPUT ヘッダー |
| `RawKeyboard` | キーボード入力データ |
| `RawInput` | `RawInputHeader` + `RawKeyboard` (union) |
| `WndClassW` | ウィンドウクラス登録用 |
| `InputStruct` | `SendInput` 用 (`INPUT_KEYBOARD` type) |
| `InputUnion` | `InputStruct` の共用体部分 |
| `KeyboardInput` | キーボード入力構造体 (`KEYBDINPUT`) |
| `KbDllHookStruct` | LL Hook コールバック用 |

---

## 変更時の影響範囲

| 変更内容 | 影響を受けるレイヤー |
|----------|----------------------|
| Native に新 API 追加 | Devices または Macros (使用箇所) |
| 構造体フィールド変更 | 使用している全サブレイヤー |
| 定数値変更 | 使用している全サブレイヤー |
| デリゲートシグネチャ変更 | Devices (RawInputHook, DeviceFilter) |

---

## テスト方針
- Native 層は直接テスト困難 (Win32 API 依存)
- Devices/Macros 層でインターフェース経由のモックテストを推奨
- 実機テストは手動で実施

---

## 既知の問題
- `KEYEVENTF_EXTENDEDKEY` は定義済みだが `KeyStrokeAction` では未使用 (一部拡張キーで問題の可能性)
- `RIDI_DEVICEINFO` は定義済みだが現在の実装では未使用 (将来のデバイス詳細取得用)
