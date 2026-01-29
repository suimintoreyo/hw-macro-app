# CLAUDE.md - Tests

## 概要
HwMacroApp のテスト方針、カバレッジ目標、テスト実装状況を管理。

## テストフレームワーク
- xUnit
- Microsoft.NET.Test.Sdk

---

## テスト段階

### Phase A: 単体テスト (Unit Tests)
**対象**: ロジックを分離してテスト可能な部分
**実行環境**: CI で自動実行可能

| テストクラス | 対象 | 状態 |
|--------------|------|------|
| `ConfigStoreTests` | JSON 読み書き | ⬜ 未実装 |
| `MacroActionFactoryTests` | ActionConfig → IMacroAction | ⬜ 未実装 |
| `DeviceConnectionTypeTests` | デバイスパス判定 | ⬜ 未実装 |
| `MacroSequenceTests` | アクション順次実行 | ⬜ 未実装 |

### Phase B: 統合テスト (Integration Tests)
**対象**: 複数コンポーネントの連携
**実行環境**: モック使用で CI 実行可能

| テストクラス | 対象 | 状態 |
|--------------|------|------|
| `MacroDispatcherTests` | キー→マクロマッピング | ⬜ 未実装 |
| `MainViewModelTests` | UI 操作→設定保存 | ⬜ 未実装 |

### Phase C: 実環境テスト (Manual / E2E)
**対象**: ハードウェア・OS 依存部分
**実行環境**: Windows 実機 (手動)

| シナリオ | 確認項目 | 状態 |
|----------|----------|------|
| デバイス接続 | 列挙更新 | ⬜ 未実施 |
| 実キー入力 | Raw Input 受信 | ⬜ 未実施 |
| 入力抑制 | LL Hook 動作 | ⬜ 未実施 |
| Bluetooth | BT/BLE 判定 | ⬜ 未実施 |
| タスクトレイ | 常駐動作 | ⬜ 未実施 |

---

## カバレッジ目標

| レイヤー | 目標 | 現在 |
|----------|------|------|
| Config | 90% | 0% |
| Macros (Factory, Sequence) | 80% | 0% |
| Devices (判定ロジックのみ) | 70% | 0% |
| App (ViewModel) | 60% | 0% |

**除外対象**:
- Native (P/Invoke)
- RawInputHook (Win32 API 直接呼び出し)
- DeviceFilter (LL Hook)
- TrayIconService (UI)

---

## モック設計

### 推奨インターフェース

```csharp
// Devices 層
public interface IRawInputService
{
    event Action<RawKeyEvent>? KeyReceived;
    void Start();
}

public interface IDeviceEnumerator
{
    IReadOnlyList<InputDeviceInfo> GetConnectedKeyboards();
}

// Macros 層
public interface ISendInputService
{
    void SendKeystrokes(ushort[] keys, ushort[] modifiers);
}

// Config 層
public interface IConfigStore
{
    AppConfig Load();
    void Save(AppConfig config);
}
```

### モック実装例

```csharp
public class MockConfigStore : IConfigStore
{
    public AppConfig Config { get; set; } = new();
    public bool SaveCalled { get; private set; }

    public AppConfig Load() => Config;
    public void Save(AppConfig config) { Config = config; SaveCalled = true; }
}
```

---

## テストデータ

### デバイスパス例

```csharp
public static class TestDevicePaths
{
    public const string Usb = @"\\?\HID#VID_1234&PID_5678#...";
    public const string Bluetooth = @"\\?\HID#BTHENUM#Dev_AABBCCDD#...";
    public const string BluetoothLE = @"\\?\HID#BTHLE#Dev_AABBCCDD#...";
}
```

### 設定 JSON 例

```csharp
public static class TestConfigs
{
    public static AppConfig SingleDevice => new()
    {
        Devices = new()
        {
            new DeviceConfig
            {
                DevicePath = TestDevicePaths.Usb,
                FriendlyName = "Test Device",
                ConnectionType = DeviceConnectionType.Usb,
                Enabled = true,
                Bindings = new()
                {
                    new KeyBinding
                    {
                        VKey = 0x61, // Numpad1
                        KeyName = "Numpad1",
                        Actions = new()
                        {
                            new ActionConfig { Type = "keystroke", Keys = new[] { 0x43 } }
                        }
                    }
                }
            }
        }
    };
}
```

---

## テスト実装ガイドライン

### 命名規則
```
[メソッド名]_[シナリオ]_[期待結果]

例:
- Load_FileNotExists_ReturnsEmptyConfig
- DetermineConnectionType_BthEnumPath_ReturnsBluetooth
- Create_KeystrokeType_ReturnsKeyStrokeAction
```

### Arrange-Act-Assert パターン
```csharp
[Fact]
public void DetermineConnectionType_BthEnumPath_ReturnsBluetooth()
{
    // Arrange
    var path = @"\\?\HID#BTHENUM#...";

    // Act
    var result = DeviceEnumerator.DetermineConnectionType(path);

    // Assert
    Assert.Equal(DeviceConnectionType.Bluetooth, result);
}
```

---

## CI 設定 (将来)

```yaml
# .github/workflows/test.yml
name: Test
on: [push, pull_request]
jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet test --verbosity normal
```

---

## 更新履歴

| 日付 | 内容 |
|------|------|
| (初版) | テスト方針策定、Phase A-C 定義 |

---

## 次のアクション
1. Phase A の単体テスト実装
2. インターフェース抽出 (テスタビリティ向上)
3. CI パイプライン設定
