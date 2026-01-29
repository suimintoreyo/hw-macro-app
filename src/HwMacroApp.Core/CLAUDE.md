# CLAUDE.md - HwMacroApp.Core

## 概要
コアロジックを提供するクラスライブラリ。UI 非依存。

## 依存関係
```
このレイヤー → 依存先
━━━━━━━━━━━━━━━━━━━━━━━━━
Macros   → Config, Native
Devices  → Native
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
- `Native/NativeMethods.cs` — P/Invoke 関数宣言
- `Native/RawInputStructs.cs` — Win32 構造体定義

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

### 定数定義

```csharp
// メッセージ
WM_INPUT = 0x00FF
WM_DEVICECHANGE = 0x0219
WM_KEYDOWN = 0x0100
WM_KEYUP = 0x0101

// Raw Input
RIDEV_INPUTSINK = 0x00000100
RID_INPUT = 0x10000003
RIM_TYPEKEYBOARD = 1
RIDI_DEVICENAME = 0x20000007

// HID Usage
HID_USAGE_PAGE_GENERIC = 0x01
HID_USAGE_GENERIC_KEYBOARD = 0x06

// Hook
WH_KEYBOARD_LL = 13

// SendInput
INPUT_KEYBOARD = 1
KEYEVENTF_KEYUP = 0x0002
```

### 構造体一覧

| 構造体 | 用途 |
|--------|------|
| `RawInputDevice` | RegisterRawInputDevices 用 |
| `RawInputDeviceList` | GetRawInputDeviceList 用 |
| `RawInputHeader` | RAWINPUT ヘッダー |
| `RawKeyboard` | キーボード入力データ |
| `RawInput` | RawInputHeader + RawKeyboard |
| `WndClassW` | ウィンドウクラス登録用 |
| `InputStruct` | SendInput 用 |
| `KeyboardInput` | キーボード入力構造体 |
| `KbDllHookStruct` | LL Hook コールバック用 |

---

## 変更時の影響範囲

| 変更内容 | 影響を受けるレイヤー |
|----------|----------------------|
| Native に新 API 追加 | Devices または Macros (使用箇所) |
| 構造体フィールド変更 | 使用している全サブレイヤー |
| 定数値変更 | 使用している全サブレイヤー |

---

## テスト方針
- Native 層は直接テスト困難 (Win32 API 依存)
- Devices/Macros 層でインターフェース経由のモックテストを推奨
- 実機テストは手動で実施

---

## 既知の問題
(現時点でなし)
