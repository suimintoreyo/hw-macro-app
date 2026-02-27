# CLAUDE.md - Macros レイヤー

## 概要
マクロアクションの定義と実行を担当。キーストローク送信、アプリ起動、スクリプト実行、遅延などをサポート。

## 依存関係
```
Macros → Config (ActionConfig)
Macros → Native (SendInput, InputStruct)
```

---

## ファイル構成

| ファイル | 役割 |
|----------|------|
| `IMacroAction.cs` | アクション共通インターフェース |
| `KeyStrokeAction.cs` | `SendInput` でキーストローク送信 |
| `LaunchAppAction.cs` | `Process.Start` でアプリ起動 (UseShellExecute=true) |
| `ScriptAction.cs` | PowerShell スクリプト実行 (UseShellExecute=false, configurable shell) |
| `DelayAction.cs` | `Task.Delay` で遅延 |
| `MacroSequence.cs` | 複数アクションの順次実行 |
| `MacroDispatcher.cs` | キーイベント → マクロ実行のマッピング |
| `MacroActionFactory.cs` | `ActionConfig` → `IMacroAction` 生成 |

---

## 公開 API

### IMacroAction

```csharp
public interface IMacroAction
{
    string DisplayName { get; }
    Task ExecuteAsync(CancellationToken ct = default);
}
```

### アクション実装

| クラス | DisplayName 例 | 動作 |
|--------|----------------|------|
| `KeyStrokeAction` | `0xA2+0x43` | Modifier + Key を SendInput で送信 |
| `LaunchAppAction` | `notepad.exe` | UseShellExecute=true で起動 (非同期、終了待たない) |
| `ScriptAction` | `Script: test.ps1` | PowerShell -ExecutionPolicy Bypass (プロセス終了まで await) |
| `DelayAction` | `Delay 100ms` | `Task.Delay(Milliseconds, ct)` |
| `MacroSequence` | `A → B → C` | 順次実行、各ステップで CancellationToken チェック |

### MacroDispatcher

```csharp
public sealed class MacroDispatcher
{
    public MacroDispatcher(AppConfig config);
    public async Task HandleKeyEventAsync(RawKeyEvent evt);
}
```

**処理フロー**:
1. `evt.IsKeyUp == true` なら無視 (KeyDown のみ処理)
2. `evt.DevicePath` で `config.Devices` から有効デバイスを検索 (OrdinalIgnoreCase)
3. `evt.VKey` でキーバインドを検索
4. `MacroActionFactory` で各 `ActionConfig` からアクション生成
5. `MacroSequence.ExecuteAsync()` で順次実行

**注意**: `_factory` は内部で `new MacroActionFactory()` として保持。設定変更後は `MacroDispatcher` を再生成する必要あり。

### MacroActionFactory

```csharp
public sealed class MacroActionFactory
{
    public IMacroAction Create(ActionConfig config);
}
```

**対応 type 値** (大文字小文字無視 — `ToLowerInvariant()` で比較):

| type 値 | 生成クラス | 必須フィールド |
|---------|-----------|----------------|
| `"keystroke"` | `KeyStrokeAction` | `config.Keys` |
| `"launchapp"` | `LaunchAppAction` | `config.Path` |
| `"script"` | `ScriptAction` | `config.Path` |
| `"delay"` | `DelayAction` | `config.Milliseconds` (デフォルト 100ms) |

不明な type は `NotSupportedException` をスロー。

---

## KeyStrokeAction 詳細

### プロパティ
```csharp
public required ushort[] Keys { get; init; }    // メインキー
public ushort[] Modifiers { get; init; } = [];  // 修飾キー (省略可)
```

### 送信順序
```
1. Modifiers KeyDown (配列順)
2. Keys KeyDown (配列順)
3. Keys KeyUp (逆順)
4. Modifiers KeyUp (逆順)
```

### 例: Ctrl+C (Modifier=0xA2, Key=0x43)
```
KeyDown: 0xA2 (LControlKey)
KeyDown: 0x43 (C)
KeyUp:   0x43 (C)
KeyUp:   0xA2 (LControlKey)
```

### Virtual Key コード (よく使うもの)
| キー | VK コード |
|------|-----------|
| LCtrl | 0xA2 |
| RCtrl | 0xA3 |
| LShift | 0xA0 |
| RShift | 0xA1 |
| LAlt | 0xA4 |
| RAlt | 0xA5 |
| A-Z | 0x41-0x5A |
| 0-9 | 0x30-0x39 |
| Numpad 0-9 | 0x60-0x69 |
| F1-F12 | 0x70-0x7B |

---

## ScriptAction 詳細

```csharp
public required string ScriptPath { get; init; }
public string Shell { get; init; } = "powershell.exe";       // 変更可能
public string Arguments { get; init; } = "-ExecutionPolicy Bypass -File";  // 変更可能
```

実行コマンド: `{Shell} {Arguments} "{ScriptPath}"`

- `UseShellExecute = false` (シェルを経由しない)
- `CreateNoWindow = true` (ウィンドウ非表示)
- プロセス終了まで `WaitForExitAsync(ct)` で待機

---

## テスト方針

### 単体テスト可能
- `MacroActionFactory.Create()` — 各 type の生成確認、不正 type の例外
- `MacroSequence` — アクション順序の確認 (モックアクション使用)
- `MacroDispatcher` — キー→マクロマッピング (モック Config 使用)
- `DelayAction` — DisplayName の確認

### モック対象
```csharp
// 提案: SendInput を抽象化してテスト可能に
public interface ISendInputService
{
    void SendKeystrokes(ushort[] allKeys);
}
```

### 手動テスト必要
- 実際のキーストローク送信確認
- アプリ起動確認
- スクリプト実行確認

---

## 将来の拡張
- [ ] `TextInputAction` — 文字列直接入力 (Unicode / `KEYEVENTF_UNICODE`)
- [ ] `MouseAction` — マウスクリック/移動
- [ ] `ConditionalAction` — 条件分岐
- [ ] `RepeatAction` — 繰り返し実行
- [ ] `HotkeyAction` — 別ホットキー発火
- [ ] cmd.exe / bash 等への ScriptAction 対応

---

## 既知の問題
- `ScriptAction` の `Shell` / `Arguments` は `ActionConfig` に対応するフィールドがなく、常にデフォルト値 (PowerShell)
- `KeyStrokeAction` の `KEYEVENTF_EXTENDEDKEY` フラグ未対応 (PrintScreen, Insert, Delete 等の拡張キーで問題の可能性)
- `MacroDispatcher` はコンストラクタ時の `AppConfig` を参照するため、設定変更後は再生成が必要
