# CLAUDE.md - Devices レイヤー

## 概要
USB / Bluetooth デバイスの列挙、Raw Input によるキー入力取得、Low-Level Hook による入力抑制を担当。

## 依存関係
```
Devices → Native (P/Invoke)
```

## ファイル構成

| ファイル | 役割 |
|----------|------|
| `RawInputHook.cs` | WM_INPUT を受信してキーイベントを発行 |
| `DeviceEnumerator.cs` | 接続中 HID キーボードの列挙 |
| `DeviceFilter.cs` | 登録デバイスからの入力を LL Hook で抑制 |
| `DeviceConnectionType.cs` | 接続種別 enum |
| `InputDeviceInfo.cs` | デバイス情報 record |
| `RawKeyEvent.cs` | キーイベント record |

---

## 公開 API

### RawInputHook

```csharp
public sealed class RawInputHook : IDisposable
{
    public event Action<RawKeyEvent>? KeyReceived;
    public void Start();
    public void Dispose();
}
```

**動作**:
1. メッセージ専用ウィンドウを作成 (`HWND_MESSAGE`)
2. `RegisterRawInputDevices` でキーボード HID を登録
3. `WM_INPUT` 受信時に `RawKeyEvent` を発火

**注意**:
- UI スレッド (Dispatcher) から `Start()` を呼ぶこと
- `_wndProcDelegate` を保持して GC を防止

### DeviceEnumerator

```csharp
public sealed partial class DeviceEnumerator
{
    public IReadOnlyList<InputDeviceInfo> GetConnectedKeyboards();
    internal static DeviceConnectionType DetermineConnectionType(string devicePath);
}
```

**接続種別判定ロジック**:
```
デバイスパスに "BTHLE" を含む → BluetoothLE
デバイスパスに "BTHENUM" を含む → Bluetooth
それ以外 → Usb
```

**VID/PID 抽出**: 正規表現 `VID_([0-9A-Fa-f]{4})`, `PID_([0-9A-Fa-f]{4})`

### DeviceFilter

```csharp
public sealed class DeviceFilter : IDisposable
{
    public void Register(string devicePath);
    public void Unregister(string devicePath);
    public bool IsRegistered(string devicePath);
    public void SetLastDevice(string devicePath);
    public void StartHook();
    public void Dispose();
}
```

**入力抑制の仕組み**:
1. `RawInputHook.KeyReceived` で `SetLastDevice()` を呼び出し
2. LL Hook コールバックで `_lastRawInputDevicePath` を参照
3. 登録済みデバイスなら `CallNextHookEx` を呼ばず `(IntPtr)1` を返す

**制限**:
- WM_INPUT と LL Hook の処理順序に依存
- 高速なキー連打時にタイミングずれの可能性あり

### データ型

```csharp
public enum DeviceConnectionType { Usb, UsbWireless, Bluetooth, BluetoothLE }

public sealed record InputDeviceInfo(
    string DevicePath,
    string FriendlyName,
    ushort VendorId,
    ushort ProductId,
    DeviceConnectionType ConnectionType);

public sealed record RawKeyEvent(
    IntPtr DeviceHandle,
    string DevicePath,
    ushort VKey,
    ushort ScanCode,
    bool IsKeyUp);
```

---

## Bluetooth デバイス対応

### デバイスパス例
```
USB:       \\?\HID#VID_1234&PID_5678#...
Bluetooth: \\?\HID#BTHENUM#...
BLE:       \\?\HID#BTHLE#...
```

### 既知の動作
- Bluetooth デバイスも Raw Input API で同様にキー入力を取得可能
- 接続/切断の検知は `WM_DEVICECHANGE` (未実装)
- スリープ復帰後はデバイスパスが変わる可能性あり → VID/PID で再マッチング推奨

---

## テスト方針

### 単体テスト可能
- `DetermineConnectionType()` — 文字列パターンマッチのみ
- VID/PID 抽出ロジック

### モック必要
- `RawInputHook` — `IRawInputService` インターフェース抽出で対応可能
- `DeviceFilter` — `IKeyboardHookService` インターフェース抽出で対応可能

### 手動テスト必要
- 実デバイスでの Raw Input 受信確認
- 入力抑制の動作確認
- Bluetooth デバイスの接続/切断

---

## 将来の拡張
- [ ] `WM_DEVICECHANGE` によるデバイス着脱検知
- [ ] USB ワイヤレス (2.4GHz ドングル) の細分化判定
- [ ] Interception ドライバ対応 (代替入力抑制方式)

---

## 既知の問題
- LL Hook の `_lastRawInputDevicePath` は volatile だが、マルチスレッド競合の可能性あり
- 高頻度キー入力時のタイミング問題は未検証
