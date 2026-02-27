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
1. `_wndProcDelegate` を保持 (GC 防止)
2. ランダムクラス名でウィンドウクラスを登録 (`RegisterClassW`)
3. `HWND_MESSAGE` 親でメッセージ専用ウィンドウを作成 (`CreateWindowExW`)
4. `RegisterRawInputDevices` (RIDEV_INPUTSINK) でキーボード HID を登録
5. `WM_INPUT` 受信時に `RawKeyEvent` を発火

**注意**:
- UI スレッド (Dispatcher) から `Start()` を呼ぶこと
- `_wndProcDelegate` を保持して GC を防止している

**内部実装詳細**:
- `ProcessRawInput`: `GetRawInputData` で RAWINPUT 構造体取得、`Marshal.AllocHGlobal` でバッファ確保 (finally で解放)
- `GetDevicePath`: `GetRawInputDeviceInfoW(RIDI_DEVICENAME)` でデバイスパス取得
- isKeyUp 判定: `raw.Keyboard.Flags & 0x01`

### DeviceEnumerator

```csharp
public sealed partial class DeviceEnumerator  // partial は GeneratedRegex のため
{
    public IReadOnlyList<InputDeviceInfo> GetConnectedKeyboards();
    internal static DeviceConnectionType DetermineConnectionType(string devicePath);
}
```

**接続種別判定ロジック** (`DetermineConnectionType`):
```
devicePath.ToUpperInvariant() に "BTHLE"  を含む → BluetoothLE
devicePath.ToUpperInvariant() に "BTHENUM" を含む → Bluetooth
それ以外 → Usb  (USB ワイヤレスドングルも Usb に統合)
```

**VID/PID 抽出**: `[GeneratedRegex]` による source-generated regex
- `VID_([0-9A-Fa-f]{4})` → VendorId
- `PID_([0-9A-Fa-f]{4})` → ProductId

**FriendlyName 生成**: `{BT|BLE|USB} Keyboard ({VID:X4}:{PID:X4})` 形式

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

**内部状態**:
- `_registeredPaths`: `HashSet<string>(StringComparer.OrdinalIgnoreCase)` — 大文字小文字無視
- `_lastRawInputDevicePath`: `volatile string?` — WM_INPUT → LL Hook 間でデバイスパスを受け渡し

**入力抑制の仕組み**:
1. `RawInputHook.KeyReceived` → `App.OnKeyReceived` → `DeviceFilter.SetLastDevice(path)` で直近デバイス記録
2. LL Hook コールバック (`HookCallback`) で `_lastRawInputDevicePath` を参照
3. 登録済みデバイスなら `CallNextHookEx` を呼ばず `(IntPtr)1` を返す

**制限**:
- WM_INPUT と LL Hook の処理順序に依存 (WM_INPUT が先に処理されることを前提)
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
    ushort ScanCode,   // MakeCode フィールドから取得
    bool IsKeyUp);
```

---

## Bluetooth デバイス対応

### デバイスパス例
```
USB:         \\?\HID#VID_1234&PID_5678#...
Bluetooth:   \\?\HID#BTHENUM#Dev_AABBCCDD#...
BluetoothLE: \\?\HID#BTHLE#Dev_AABBCCDD#...
```

### 既知の動作
- Bluetooth デバイスも Raw Input API で同様にキー入力を取得可能
- 接続/切断の検知は `WM_DEVICECHANGE` (未実装)
- スリープ復帰後はデバイスパスが変わる可能性あり → VID/PID で再マッチング推奨

---

## テスト方針

### 単体テスト可能
- `DetermineConnectionType()` — 文字列パターンマッチのみ、Win32 API 不要
- VID/PID 抽出ロジック — 正規表現テスト

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
- [ ] USB ワイヤレス (2.4GHz ドングル) の `UsbWireless` への細分化判定
- [ ] Interception ドライバ対応 (代替入力抑制方式)

---

## 既知の問題
- `DeviceFilter._lastRawInputDevicePath` は `volatile` だが、マルチスレッド競合の可能性あり
- 高頻度キー入力時のタイミング問題は未検証
- `UsbWireless` enum 値は定義済みだが、実際には `DetermineConnectionType` で使用されていない (Usb に統合)
