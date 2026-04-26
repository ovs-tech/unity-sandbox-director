[English](CONTRIBUTING.md) | [Tiếng Việt](CONTRIBUTING.vi.md) | [中文](CONTRIBUTING.zh.md) | [日本語](CONTRIBUTING.ja.md) | [한국어](CONTRIBUTING.ko.md)

# 🎥 Contributing to Director Simulator: Scene Builder

Welcome to the Unity Sandbox Director community! Below is how to set up the dev environment, run tests, and submit PRs.

---

## 🛠️ Setup
1. **Unity** – version `2021.3` or newer (URP recommended).
2. **Git** – clone the repo and checkout the `main` branch.
3. Open the project in Unity. Unity will automatically import the necessary packages.
4. Run the demo scene: `File → Open Scene → Assets/Scenes/Demo.unity` and press Play.

---

## 📁 Repository Structure
```
Assets/
  Scripts/MiniTimeline/        # Core timeline system
  Scripts/Placement/           # Placement Editor Framework
  Scenes/                      # Demo and test scenes
  Resources/                   # ScriptableObjects, prefabs
  Editor/                      # Editor extensions (Prefab Creator, UI)
docs/                          # Technical docs, GDD, design notes
.github/                       # Templates for issues & PRs
```

---

## 🧪 Test & Validation
- Run **Mini Timeline → Test Serialization** in the Unity Editor to check save/load.
- Run **Placement System → Validate Rules** to check placement rules.
- Ensure the build has no errors before committing.

---

## 📝 Coding Standards
- C# follows Unity conventions, file names in PascalCase, namespaces follow directory structure.
- Keep comments concise, prefer XML docs for public APIs.
- Commit message format: `[System] Short description` e.g., `[Timeline] Fix event scrubbing lag`.

---

## 🚀 Pull Request
1. Create a new branch from `main`: `feature/your-feature-name` or `fix/bug-description`.
2. Write tests (if possible) and ensure Unity build is successful.
3. Open a PR with the template (`.github/PULL_REQUEST_TEMPLATE.md`).
4. Reviewers will respond within 48h. If there's no response, ping `@maintainer`.

---

## 🎬 Good First Issues
- The `good-first-issue` label is prioritized for beginners.
- Comment "I'm working on this" before starting to avoid duplication.

---

Thank you for contributing to making Director Simulator even better! 🌟