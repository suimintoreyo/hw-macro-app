# CLAUDE.md - プロジェクトメモリ

## プロジェクト概要
Windows 向け USB / Bluetooth デバイス常駐マクロソフトウェア (C# / .NET 8 / WPF)

## ブランチ戦略
- `main` — ベースブランチ (ローカル名。リモートでは `claude/usb-macro-readme-rYgrk`)
- `claude/dev-rYgrk` — 開発ブランチ (push 先)
- push 制約: `claude/` プレフィックス + セッション ID 末尾が必要

## リモート
- origin: `suimintoreyo/hw-macro-app`
- リモートのデフォルトブランチを `main` に変更する作業は未完了 (gh CLI 未使用可)

## 現在の状態
- README.md に構築プランを記載済み (USB + Bluetooth 対応)
- 実装コードはまだなし

## 技術スタック
- C# / .NET 8+
- WPF (設定画面) / NotifyIcon (タスクトレイ常駐)
- Raw Input API でデバイス別キー入力取得
- SetupAPI / WMI / BluetoothLE APIs でデバイス列挙
- SendInput API でマクロ実行
- JSON で設定保存

## キー設計判断
- キー入力抑制: Raw Input + Low-Level Keyboard Hook (`WH_KEYBOARD_LL`) 併用方式を優先
- Bluetooth デバイス識別: デバイスパスの `BTHENUM` / `BTHLE` パターンで判定
- デバイス再接続: VID/PID + デバイス名で再マッチング

## プロジェクト構成
- `src/HwMacroApp.Core/` — コアロジック (Devices, Macros, Config, Native)
- `src/HwMacroApp.App/` — WPF アプリ (Views, ViewModels, Services)
- `tests/HwMacroApp.Core.Tests/`
