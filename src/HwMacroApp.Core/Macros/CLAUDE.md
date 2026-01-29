# CLAUDE.md - Macros レイヤー

## 概要
マクロアクションの定義と実行を担当。キーストローク送信、アプリ起動、スクリプト実行、遅延などをサポート。

## 依存関係
```
Macros → Config (ActionConfig)
Macros → Native (SendInput)
```

---

## ファイル構成

| ファイル | 役割 |
|----------|------|
| `IMacroAction.cs` | アクション共通インターフェース |
| `KeyStrokeAction.cs` | SendInput でキーストローク送信 |
| `LaunchAppAction.cs` | Process.Start でアプリ起動 |
| `ScriptAction.cs` | PowerShell スクリプト実行 |
| `DelayAction.cs` | Task.Delay で遅延 |
| `MacroSequence.cs` | 複数アクションの順次実行 |
| `MacroDispatcher.cs` | キーイベント → マクロ実行のマッピング |
| `MacroActionFactory.cs` | ActionConfig → IMacroAction 生成 |

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
| `LaunchAppAction` | `notepad.exe` | UseShellExecute=true で起動 |
| `ScriptAction` | `Script: test.ps1` | PowerShell -ExecutionPolicy Bypass |
| `DelayAction` | `Delay 100ms` | Task.Delay |
| `MacroSequence` | `A → B → C` | 順次実行 |

### MacroDispatcher

```csharp
public sealed class MacroDispatcher
{
    public MacroDispatcher(AppConfig config);
    public async Task HandleKeyEventAsync(RawKeyEvent evt);
}
```

**処理フロー**:
1. `evt.IsKeyUp` なら無視 (KeyDown のみ処理)
2. `evt.DevicePath` で登録デバイスを検索
3. `evt.VKey` でキーバインドを検索
4. `MacroActionFactory` でアクション生成
5. `MacroSequence.ExecuteAsync()` で実行

### MacroActionFactory

```csharp
public sealed class MacroActionFactory
{
    public IMacroAction Create(ActionConfig config);
}
```

**対応 type 値**:
- `"keystroke"` → `KeyStrokeAction`
- `"launchapp"` → `LaunchAppAction`
- `"script"` → `ScriptAction`
- `"delay"` → `DelayAction`

---

## KeyStrokeAction 詳細

### 送信順序
```
1. Modifier KeyDown (順方向)
2. Key KeyDown (順方向)
3. Key KeyUp (逆方向)
4. Modifier KeyUp (逆方向)
```

### 例: Ctrl+C
```
KeyDown: LControlKey (0xA2)
KeyDown: C (0x43)
KeyUp:   C (0x43)
KeyUp:   LControlKey (0xA2)
```

### Virtual Key コード (よく使うもの)
| キー | VK コード |
|------|-----------|
| Ctrl | 0xA2 (Left), 0xA3 (Right) |
| Shift | 0xA0 (Left), 0xA1 (Right) |
| Alt | 0xA4 (Left), 0xA5 (Right) |
| A-Z | 0x41-0x5A |
| 0-9 | 0x30-0x39 |
| Numpad 0-9 | 0x60-0x69 |
| F1-F12 | 0x70-0x7B |

---

## テスト方針

### 単体テスト可能
- `MacroActionFactory.Create()` — 各 type の生成確認
- `MacroSequence` — アクション順序の確認 (モックアクション使用)
- `MacroDispatcher` — キー→マクロマッピング (モック Config 使用)

### モック対象
```csharp
// 提案: ISendInputService インターフェース
public interface ISendInputService
{
    void SendKeystrokes(ushort[] keys, ushort[] modifiers, bool keyUp);
}
```

### 手動テスト必要
- 実際のキーストローク送信確認
- アプリ起動確認
- スクリプト実行確認

---

## 将来の拡張
- [ ] `TextInputAction` — 文字列直接入力 (Unicode 対応)
- [ ] `MouseAction` — マウスクリック/移動
- [ ] `ConditionalAction` — 条件分岐
- [ ] `RepeatAction` — 繰り返し実行
- [ ] `HotkeyAction` — 他のホットキー発火

---

## 既知の問題
- `ScriptAction` は PowerShell 固定。cmd.exe 等への対応は未実装
- `KeyStrokeAction` の `KEYEVENTF_EXTENDEDKEY` フラグ未対応 (一部キーで問題の可能性)
