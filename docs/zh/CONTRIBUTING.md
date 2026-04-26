[English](../../CONTRIBUTING.md) | [Tiếng Việt](../vi/CONTRIBUTING.md) | [中文](../zh/CONTRIBUTING.md) | [日本語](../ja/CONTRIBUTING.md) | [한국어](../ko/CONTRIBUTING.md)

# 🎥 参与 Director Simulator: Scene Builder

欢迎来到 Unity Sandbox Director 社区！以下是设置开发环境、运行测试和提交 PR 的指南。

---

## 🛠️ 设置
1. **Unity** – 版本 `2021.3` 或更高 (推荐使用 URP)。
2. **Git** – 克隆仓库并切换到 `main` 分支。
3. 在 Unity 中打开项目。Unity 会自动导入所需包。
4. 运行演示场景: `File → Open Scene → Assets/Scenes/Demo.unity` 并点击 Play。

---

## 📁 仓库结构
```
Assets/
  Scripts/MiniTimeline/        # 核心时间线系统
  Scripts/Placement/           # 放置编辑器框架
  Scenes/                      # 演示和测试场景
  Resources/                   # ScriptableObjects, 预制件
  Editor/                      # 编辑器扩展 (预制件创建器, UI)
docs/                          # 技术文档, GDD, 设计笔记
.github/                       # Issue 和 PR 模板
```

---

## 🧪 测试和验证
- 在 Unity Editor 中运行 **Mini Timeline → Test Serialization** 以检查保存/加载功能。
- 运行 **Placement System → Validate Rules** 以检查放置规则。
- 提交前确保构建没有错误。

---

## 📝 编码规范
- C# 遵循 Unity 规范，文件名使用 PascalCase，命名空间遵循目录结构。
- 注释需简洁，推荐使用 XML 文档描述公共 API。
- 提交信息格式：`[System] 简短描述`，例如：`[Timeline] 修复事件拖动延迟`。

---

## 🚀 Pull Request
1. 从 `main` 创建一个新分支：`feature/your-feature-name` 或 `fix/bug-description`。
2. 编写测试（如果可能）并确保 Unity 构建成功。
3. 使用模板打开一个 PR (`.github/PULL_REQUEST_TEMPLATE.md`)。
4. 审查人员将在 48 小时内回复。如果没有回复，请 @maintainer。

---

## 🎬 适合初学者的问题 (Good First Issues)
- `good-first-issue` 标签优先分配给初学者。
- 在开始之前评论 "I'm working on this"，以避免重复。

---

感谢您为让 Director Simulator 变得更好所做的贡献！ 🌟