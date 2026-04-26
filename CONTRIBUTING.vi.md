[English](CONTRIBUTING.md) | [Tiếng Việt](CONTRIBUTING.vi.md) | [中文](CONTRIBUTING.zh.md) | [日本語](CONTRIBUTING.ja.md) | [한국어](CONTRIBUTING.ko.md)

# 🎥 Contributing to Director Simulator: Scene Builder

Chào mừng bạn đến với cộng đồng đạo diễn Unity Sandbox! Dưới đây là cách thiết lập môi trường dev, chạy test, và gửi PR.

---

## 🛠️ Setup
1. **Unity** – phiên bản `2021.3` hoặc mới hơn (URP được đề xuất).
2. **Git** – clone repo và checkout nhánh `main`.
3. Mở project trong Unity. Unity sẽ tự import packages cần thiết.
4. Chạy scene demo: `File → Open Scene → Assets/Scenes/Demo.unity` và nhấn Play.

---

## 📁 Cấu trúc repo
```
Assets/
  Scripts/MiniTimeline/        # Core timeline system
  Scripts/Placement/           # Placement Editor Framework
  Scenes/                      # Demo và test scenes
  Resources/                   # ScriptableObjects, prefabs
  Editor/                      # Editor extensions (Prefab Creator, UI)
docs/                          # Tài liệu kỹ thuật, GDD, design notes
.github/                       # Templates cho issues & PRs
```

---

## 🧪 Test & Validation
- Chạy **Mini Timeline → Test Serialization** trong Unity Editor để kiểm tra save/load.
- Chạy **Placement System → Validate Rules** để kiểm tra placement rules.
- Đảm bảo build không lỗi trước khi commit.

---

## 📝 Coding Standards
- C# theo convention Unity, tên file PascalCase, namespace theo cấu trúc thư mục.
- Comment ngắn gọn, ưu tiên XML doc cho public API.
- Commit message theo format: `[System] Mô tả ngắn` ví dụ: `[Timeline] Fix event scrubbing lag`.

---

## 🚀 Pull Request
1. Tạo nhánh mới từ `main`: `feature/your-feature-name` hoặc `fix/bug-description`.
2. Viết test (nếu có thể) và đảm bảo Unity build thành công.
3. Mở PR với template (`.github/PULL_REQUEST_TEMPLATE.md`).
4. Reviewer sẽ phản hồi trong 48h. Nếu không có phản hồi, ping `@maintainer`.

---

## 🎬 Good First Issues
- Label `good-first-issue` được ưu tiên cho người mới.
- Comment "I'm working on this" trước khi start để tránh trùng.

---

Cảm ơn bạn đã góp phần làm cho Director Simulator trở nên tuyệt vời hơn! 🌟