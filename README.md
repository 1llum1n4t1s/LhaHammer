# LhaForge2

LhaForge2は、Windows用の高機能な圧縮・解凍ソフトウェアです。複数の圧縮形式に対応し、圧縮解凍エンジンを内蔵しており、単体で動作可能です。エクスプローラーのコンテキストメニューや右ドラッグメニューに対応し、幅広い使い方が可能です。

**ライセンス:** MIT License
**著作権:** Copyright (c) 2005- Claybird

## 🌟 主な特徴

- **多様な圧縮形式に対応** - 20種類以上の圧縮形式をサポート
- **圧縮解凍エンジン内蔵** - 外部DLLに依存せず独立動作可能
- **プレビュー機能** - 圧縮ファイルを解凍せずに中身を確認可能
- **ファイル検証** - 圧縮されたファイルの整合性をテスト
- **エクスプローラー統合** - コンテキストメニューおよび右ドラッグメニューに対応
- **個人・商用利用可能** - MIT ライセンスで無償提供

## 📦 対応フォーマット

### 圧縮・解凍・テストの全てに対応

- ZIP
- TAR
- GZIP
- BZ2
- Zstandard (zstd)
- LZ4
- XZ
- LZMA
- 7z

### 解凍・テストのみに対応

- LZH
- ZIPX
- Microsoft Cabinet (CAB)
- ISO9660 CD-ROM Images
- RAR
- ARJ
- CPIO
- Z (compress)
- Uuencode
- BZA/GZA (via [bga32.dll](https://www.madobe.net/archiver/lib/bga32.html))

### Ver.2.0.0以降での非対応形式

- 一部のCAB形式
- [YZ1](https://www.madobe.net/archiver/lib/yz1.html)
- [JAK](https://www.madobe.net/archiver/lib/jack32.html)
- [ISH](https://www.madobe.net/archiver/lib/aish32.html)
- [GCA](https://www.madobe.net/archiver/lib/ungca32.html)
- [IMP](https://www.madobe.net/archiver/lib/unimp32.html)
- [HKI](https://www.madobe.net/archiver/lib/unhki32.html)
- [BEL](https://www.madobe.net/archiver/lib/unbel32.html)
- [ACE](https://www.madobe.net/archiver/lib/UnAceV2J.html)

## 🔨 ビルド環境

### 必要な環境

- **Visual Studio 2017** 以降
- **Windows SDK** (Windows 7以降対応)
- **Git** (サブモジュール管理のため)
- **CMake** (依存ライブラリのビルドに使用)

### ビルド手順

1. リポジトリをクローン
```bash
git clone --recursive https://github.com/Claybird/lhaforge.git
cd lhaforge
```

2. Visual Studioでソリューションを開く
```bash
LhaForge.sln
```

3. 依存ライブラリをビルド
```bash
cd dependency
# 各サブプロジェクトをビルド
```

4. LhaForgeプロジェクトをビルド
   - Visual Studioのメニューから「Build > Build Solution」を実行

### 出力バイナリ

ビルド完了後、以下の場所に実行可能ファイルが生成されます：
```
bin/Release/LhaForge.exe
bin/Debug/LhaForge.exe
```

## 🚀 使用方法

### GUI起動

1. `LhaForge.exe` をダブルクリック
2. メニューバーから「ファイル」→「開く」で圧縮ファイルを選択
3. 目的のアクション（抽出、テスト、表示）を実行

### エクスプローラー統合

1. ファイルまたはフォルダを右クリック
2. コンテキストメニューから「LhaForge」を選択
3. 圧縮または解凍を実行

### コマンドライン使用

LhaForge2はコマンドライン引数もサポートしています。詳細は `--help` オプションを参照してください：

```bash
LhaForge.exe --help
```

## 📁 プロジェクト構造

```
LhaForge/
├── main.cpp                 # メインプログラム
├── compress.cpp/h           # 圧縮機能
├── extract.cpp/h            # 解凍機能
├── ArchiverCode/            # アーカイバ関連コード
├── ConfigCode/              # 設定管理
├── FileListWindow/          # ファイルリストウィンドウ
├── Dialogs/                 # ダイアログ
└── Utilities/               # ユーティリティ関数

dependency/                  # 外部ライブラリ（サブモジュール）
├── bzip2/
├── libarchive/
├── lz4/
├── minizip-ng/
├── zlib-ng/
├── unrar/
├── win_iconv/
├── zstd/
└── xz/

icons/                       # アプリケーションアイコン
```

## 📚 使用ライブラリ

### 圧縮解凍エンジン

| ライブラリ | 説明 | リンク |
|-----------|------|--------|
| bzip2 | BZIP2圧縮形式 | https://sourceware.org/bzip2/ |
| libarchive | 汎用アーカイブ処理ライブラリ | https://github.com/libarchive/libarchive |
| lz4 | 高速圧縮アルゴリズム | https://github.com/lz4/lz4 |
| minizip-ng | ZIP形式処理ライブラリ | https://github.com/zlib-ng/minizip-ng |
| zlib-ng | 高速DEFLATE実装 | https://github.com/zlib-ng/zlib-ng |
| unrar | RAR形式デコーダー | https://github.com/Claybird/unrar |
| zstd | Zstandard圧縮形式 | https://github.com/facebook/zstd |
| xz utils (liblzma) | XZ/LZMA圧縮形式 | https://git.tukaani.org/?p=xz.git |

### ユーティリティライブラリ

| ライブラリ | 説明 | リンク |
|-----------|------|--------|
| libcharset | 文字エンコーディング検出 | https://github.com/Claybird/libcharset-msvc |
| simpleini | INI形式設定ファイル処理 | https://github.com/brofield/simpleini |
| WTL | Windows Template Library | https://sourceforge.net/projects/wtl/ |

## 🔄 バージョン履歴

### Ver.2.0.0 (2024-12-XX)

- **メジャーリライト**: 統合アーカイバプロジェクトのDLLから独立
- 圧縮解凍エンジンを内蔵化
- 複数の外部ライブラリに統合
- コンパイラをVS2017にアップグレード
- 一部の圧縮形式サポート終了（Noah B2Eスクリプト対応も終了）

### Ver.1.6.7以前

- 統合アーカイバプロジェクトのDLLを使用
- 異なるアーカイバフォーマットをサポート

## 📖 ドキュメント

詳細なヘルプやドキュメントについては、以下を参照してください：

- [GitHub Wiki](https://github.com/Claybird/lhaforge/wiki) - ユーザーガイドとFAQ
- [Issues](https://github.com/Claybird/lhaforge/issues) - バグ報告と機能リクエスト

## 🤝 貢献

バグ報告、機能リクエスト、プルリクエストを歓迎します！

### 貢献の手順

1. このリポジトリをフォーク
2. 機能ブランチを作成 (`git checkout -b feature/amazing-feature`)
3. 変更をコミット (`git commit -m 'Add amazing feature'`)
4. ブランチにプッシュ (`git push origin feature/amazing-feature`)
5. プルリクエストを作成

### 開発上の注意

- C++17以降の標準に従うコード
- Windows API を使用した開発
- 既存のコードスタイルに従う
- テストコードの追加を推奨

## 📋 既知の問題

- 特定のCabファイル形式の互換性に問題がある可能性があります
- より詳細な既知の問題については [Issues](https://github.com/Claybird/lhaforge/issues) を参照してください

## 📄 ライセンス

このプロジェクトはMIT Licenseの下で公開されています。詳細は [LICENSE](./LICENSE) ファイルを参照してください。

個人利用・商用利用を問わず、無償で使用することができます。

## 📞 サポート

- **バグ報告**: https://github.com/Claybird/lhaforge/issues
- **ヘルプ**: https://github.com/Claybird/lhaforge/wiki
- **メール**: プロジェクトのメインテイナーに連絡

## 🔗 関連リンク

- [Original Repository](https://github.com/Claybird/lhaforge)
- [統合アーカイバプロジェクト](https://www.madobe.net/archiver/)
- [Windows Archive Manager Utility](https://github.com/Claybird)

---

**LhaForge2** - Windows用圧縮解凍ソフト
Made with ❤️ by Claybird and contributors
