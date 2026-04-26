[English](../../CONTRIBUTING.md) | [Tiếng Việt](../vi/CONTRIBUTING.md) | [中文](../zh/CONTRIBUTING.md) | [日本語](../ja/CONTRIBUTING.md) | [한국어](../ko/CONTRIBUTING.md)

# 🎥 Director Simulator: Scene Builder への貢献

Unity Sandbox Director コミュニティへようこそ！開発環境のセットアップ、テストの実行、およびPRの送信方法は以下の通りです。

---

## 🛠️ セットアップ
1. **Unity** – バージョン `2021.3` 以降（URP を推奨）。
2. **Git** – リポジトリをクローンし、`main` ブランチをチェックアウトします。
3. Unity でプロジェクトを開きます。Unity が必要なパッケージを自動的にインポートします。
4. デモシーンを実行します：`File → Open Scene → Assets/Scenes/Demo.unity` を開き、Playを押します。

---

## 📁 リポジトリ構成
```
Assets/
  Scripts/MiniTimeline/        # コアとなるタイムラインシステム
  Scripts/Placement/           # 配置エディターフレームワーク
  Scenes/                      # デモとテストシーン
  Resources/                   # ScriptableObject、プレハブ
  Editor/                      # エディター拡張機能（プレハブ作成ツール、UI）
docs/                          # 技術ドキュメント、GDD、デザインノート
.github/                       # IssueとPRのテンプレート
```

---

## 🧪 テストと検証
- Unity Editor で **Mini Timeline → Test Serialization** を実行し、セーブ/ロード機能を確認します。
- **Placement System → Validate Rules** を実行し、配置ルールを確認します。
- コミットする前にビルドエラーがないことを確認します。

---

## 📝 コーディング規約
- C# は Unity の規約に従い、ファイル名は PascalCase、名前空間はディレクトリ構造に従います。
- コメントは簡潔にし、パブリック API には XML ドキュメントを推奨します。
- コミットメッセージの形式：`[System] 短い説明` 例：`[Timeline] イベントスクラブの遅延を修正`。

---

## 🚀 Pull Request
1. `main` から新しいブランチを作成します：`feature/your-feature-name` または `fix/bug-description`。
2. テストを作成し（可能であれば）、Unityのビルドが成功することを確認します。
3. テンプレート（`.github/PULL_REQUEST_TEMPLATE.md`）を使用して PR を作成します。
4. レビュアーは 48 時間以内に応答します。応答がない場合は `@maintainer` にメンションしてください。

---

## 🎬 初心者向けの問題 (Good First Issues)
- `good-first-issue` ラベルは初心者のために優先されます。
- 作業が重複しないよう、開始する前に「I'm working on this」とコメントしてください。

---

Director Simulator をより良いものにするための貢献に感謝します！ 🌟