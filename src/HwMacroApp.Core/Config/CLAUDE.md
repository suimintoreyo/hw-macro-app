# CLAUDE.md - Config レイヤー

## 概要
アプリケーション設定のモデル定義と JSON 永続化を担当。

## 依存関係
```
Config → (なし)
※ DeviceConfig が Devices/DeviceConnectionType を参照
```

---

## ファイル構成

| ファイル | 役割 |
|----------|------|
| `AppConfig.cs` | ルート設定モデル |
| `DeviceConfig.cs` | デバイス単位の設定 (JsonStringEnumConverter 使用) |
| `KeyBinding.cs` | キーバインド定義 |
| `ActionConfig.cs` | マクロアクション設定 |
| `GeneralConfig.cs` | 全般設定 |
| `ConfigStore.cs` | JSON ファイル読み書き |

---

## データモデル

### クラス階層
```
AppConfig
├── [JsonPropertyName("devices")] Devices: List<DeviceConfig>
│   ├── [JsonPropertyName("devicePath")]     DevicePath: string
│   ├── [JsonPropertyName("friendlyName")]   FriendlyName: string
│   ├── [JsonPropertyName("connectionType")] ConnectionType: DeviceConnectionType
│   │     → [JsonConverter(typeof(JsonStringEnumConverter))] で文字列 ("Usb" 等)
│   ├── [JsonPropertyName("enabled")]        Enabled: bool (default: true)
│   └── [JsonPropertyName("bindings")]       Bindings: List<KeyBinding>
│       ├── [JsonPropertyName("vkey")]       VKey: ushort
│       ├── [JsonPropertyName("keyName")]    KeyName: string
│       └── [JsonPropertyName("actions")]    Actions: List<ActionConfig>
│           ├── [JsonPropertyName("type")]        Type: string (required)
│           ├── [JsonPropertyName("keys")]         Keys: int[]?
│           ├── [JsonPropertyName("modifiers")]    Modifiers: int[]?
│           ├── [JsonPropertyName("path")]         Path: string?
│           ├── [JsonPropertyName("args")]         Args: string?
│           └── [JsonPropertyName("ms")]           Milliseconds: int?
└── [JsonPropertyName("general")] General: GeneralConfig
    ├── [JsonPropertyName("startWithWindows")]       StartWithWindows: bool
    └── [JsonPropertyName("showNotificationOnMacro")] ShowNotificationOnMacro: bool
```

**注意**: すべてのモデルクラスは `[JsonPropertyName]` 属性で JSON キー名を明示指定している。

---

## JSON スキーマ

### 設定ファイルパス
```
%LOCALAPPDATA%\HwMacroApp\config.json
```

### JSON 例
```json
{
  "devices": [
    {
      "devicePath": "\\\\?\\HID#VID_1234&PID_5678#...",
      "friendlyName": "USB Keyboard (1234:5678)",
      "connectionType": "Usb",
      "enabled": true,
      "bindings": [
        {
          "vkey": 97,
          "keyName": "Numpad1",
          "actions": [
            {
              "type": "keystroke",
              "keys": [67],
              "modifiers": [162]
            },
            {
              "type": "delay",
              "ms": 100
            }
          ]
        }
      ]
    },
    {
      "devicePath": "\\\\?\\HID#BTHENUM#...",
      "friendlyName": "BT Keyboard (ABCD:EF01)",
      "connectionType": "Bluetooth",
      "enabled": true,
      "bindings": []
    }
  ],
  "general": {
    "startWithWindows": false,
    "showNotificationOnMacro": false
  }
}
```

### ActionConfig type 値

| type | 必須フィールド | オプション |
|------|----------------|------------|
| `keystroke` | `keys` (int[]) | `modifiers` (int[]) |
| `launchapp` | `path` (string) | `args` (string) |
| `script` | `path` (string) | — |
| `delay` | `ms` (int) | — (null の場合 MacroActionFactory で 100ms デフォルト) |

---

## 公開 API

### ConfigStore

```csharp
public sealed class ConfigStore
{
    public ConfigStore(string? filePath = null);  // null → %LOCALAPPDATA%\HwMacroApp\config.json
    public string FilePath { get; }
    public AppConfig Load();
    public void Save(AppConfig config);
}
```

**動作**:
- `Load()`: ファイルが存在しなければ `new AppConfig()` を返す (例外なし)
- `Save()`: ディレクトリが存在しなければ `Directory.CreateDirectory()` で作成

**JSON シリアライズオプション**:
```csharp
WriteIndented = true
PropertyNamingPolicy = JsonNamingPolicy.CamelCase
// (各プロパティの [JsonPropertyName] 属性が優先)
```

---

## テスト方針

### 単体テスト可能
- `ConfigStore.Load()` — 存在しないファイル、空ファイル、正常ファイル
- `ConfigStore.Save()` — シリアライズ結果の JSON 検証
- 各 JSON フィールドのデシリアライズ正確性

### テスト用ヘルパー
```csharp
var tempPath = Path.GetTempFileName();
var store = new ConfigStore(tempPath);
// テスト後に File.Delete(tempPath)
```

---

## バージョニング方針

現時点ではバージョン管理なし。将来の拡張時:

```json
{ "version": 2, "devices": [...], "general": {...} }
```

`version` フィールドがない場合は v1 として扱い、マイグレーションロジックを `ConfigStore` に実装。

---

## 将来の拡張
- [ ] 設定バージョン管理とマイグレーション
- [ ] 設定のインポート/エクスポート
- [ ] プロファイル切り替え (複数設定セット)
- [ ] アトミック書き込み (クラッシュ耐性)

---

## 既知の問題
- `DeviceConfig.ConnectionType` の `JsonStringEnumConverter` は大文字小文字が厳密 (`"Usb"` ≠ `"usb"`)
- ファイル書き込み中のクラッシュ対策 (アトミック書き込み) は未実装
- `GeneralConfig.StartWithWindows` は設定に保存されるが、実際の Windows スタートアップ登録は未実装 (Phase 6)
