# CLAUDE.md - プロジェクトメモリ (ルート)

## プロジェクト概要
Windows 向け USB / Bluetooth デバイス常駐マクロソフトウェア (C# / .NET 8 / WPF)

## ブランチ戦略
- `main` — ベースブランチ
- `claude/dev-rYgrk` — 開発ブランチ (push 先)
- push 制約: `claude/` プレフィックス + セッション ID 末尾が必要

## リモート
- origin: `suimintoreyo/hw-macro-app`

## 技術スタック
| 項目 | 技術 |
|------|------|
| 言語 | C# / .NET 8+ |
| UI | WPF (設定画面) / NotifyIcon (タスクトレイ常駐) |
| 入力取得 | Raw Input API (`RegisterRawInputDevices`) |
| 入力抑制 | Low-Level Keyboard Hook (`WH_KEYBOARD_LL`) |
| デバイス識別 | SetupAPI / デバイスパス解析 |
| マクロ実行 | SendInput API / Process.Start |
| 設定保存 | JSON (`System.Text.Json`) |

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
         ├── Macros ──→ Config, Native
         ├── Config ──→ (依存なし)
         ├── Devices ──→ Native
         └── Native ──→ (依存なし, Win32 API)
```

---

## メモリファイル構成

| ファイル | 役割 |
|----------|------|
| `/CLAUDE.md` (本ファイル) | 全体方針、アーキテクチャ、更新ルール |
| `/src/HwMacroApp.Core/CLAUDE.md` | Core 層の設計方針、Native 定義 |
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

### 完了
- [x] Phase 1: 基盤構築 (ソリューション、P/Invoke)
- [x] Phase 2: コアエンジン (RawInputHook, DeviceFilter, MacroDispatcher)
- [x] Phase 3: UI 骨格 (タスクトレイ、デバイス設定画面)

### 未完了
- [ ] Phase 4: キーバインド設定画面
- [ ] Phase 5: マクロエディタ画面
- [ ] Phase 6: Windows スタートアップ登録
- [ ] Phase 7: テスト実装
- [ ] Phase 8: インストーラ作成

---

## キー設計判断 (設計変更時は全レイヤーへ通知)

| 判断事項 | 決定内容 | 理由 |
|----------|----------|------|
| 入力抑制方式 | Raw Input + LL Hook 併用 | ドライバ不要、管理者権限で動作 |
| Bluetooth 判定 | デバイスパスの `BTHENUM`/`BTHLE` パターン | 追加 API 不要で判定可能 |
| 設定形式 | JSON | 人間が読める、.NET 標準サポート |
| UI フレームワーク | WPF + NotifyIcon | Windows ネイティブ、タスクトレイ対応 |
