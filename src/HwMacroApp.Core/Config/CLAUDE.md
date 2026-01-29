# CLAUDE.md - Config レイヤー

## 概要
アプリケーション設定のモデル定義と JSON 永続化を担当。

## 依存関係
```
Config → (なし)
※ Devices/DeviceConnectionType を参照
```

---

## ファイル構成

| ファイル | 役割 |
|----------|------|
| `AppConfig.cs` | ルート設定モデル |
| `DeviceConfig.cs` | デバイス単位の設定 |
| `KeyBinding.cs` | キーバインド定義 |
| `ActionConfig.cs` | マクロアクション設定 |
| `GeneralConfig.cs` | 全般設定 |
| `ConfigStore.cs` | JSON ファイル読み書き |

---

## データモデル

### クラス階層
```
AppConfig
├── Devices: List<DeviceConfig>
│   ├── DevicePath: string
│   ├── FriendlyName: string
│   ├── ConnectionType: DeviceConnectionType
│   ├── Enabled: bool
│   └── Bindings: List<KeyBinding>
│       ├── VKey: ushort
│       ├── KeyName: string
│       └── Actions: List<ActionConfig>
│           ├── Type: string
│           ├── Keys: int[]?
│           ├── Modifiers: int[]?
│           ├── Path: string?
│           ├── Args: string?
│           └── Milliseconds: int?
└── General: GeneralConfig
    ├── StartWithWindows: bool
    └── ShowNotificationOnMacro: bool
```

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
      "friendlyName": "USB テンキー",
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
            }
          ]
        }
      ]
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
| `keystroke` | `keys` | `modifiers` |
| `launchapp` | `path` | `args` |
| `script` | `path` | — |
| `delay` | `ms` | — |

---

## 公開 API

### ConfigStore

```csharp
public sealed class ConfigStore
{
    public ConfigStore(string? filePath = null);
    public string FilePath { get; }
    public AppConfig Load();
    public void Save(AppConfig config);
}
```

**動作**:
- `Load()`: ファイルが存在しなければ空の `AppConfig` を返す
- `Save()`: ディレクトリが存在しなければ作成

**JSON オプション**:
```csharp
WriteIndented = true
PropertyNamingPolicy = JsonNamingPolicy.CamelCase
```

---

## テスト方針

### 単体テスト可能
- `ConfigStore.Load()` — 存在しないファイル、空ファイル、正常ファイル
- `ConfigStore.Save()` — シリアライズ結果の検証
- JSON デシリアライズ — 各フィールドの正確性

### テスト用ヘルパー
```csharp
// 一時ファイルを使用
var tempPath = Path.GetTempFileName();
var store = new ConfigStore(tempPath);
```

---

## バージョニング方針

現時点ではバージョン管理なし。将来の拡張時:

```json
{
  "version": 2,
  "devices": [...],
  "general": {...}
}
```

マイグレーション:
- `version` フィールドがない場合は v1 として扱う
- v1 → v2 変換ロジックを `ConfigStore` に実装

---

## 将来の拡張
- [ ] 設定バージョン管理
- [ ] 設定のインポート/エクスポート
- [ ] プロファイル切り替え (複数設定セット)
- [ ] 設定の暗号化 (機密情報がある場合)

---

## 既知の問題
- `DeviceConnectionType` の JSON シリアライズに `JsonStringEnumConverter` を使用
- ファイル書き込み中のクラッシュ対策 (アトミック書き込み) は未実装
