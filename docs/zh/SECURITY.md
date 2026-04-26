[English](../../SECURITY.md) | [Tiếng Việt](../vi/SECURITY.md) | [中文](../zh/SECURITY.md) | [日本語](../ja/SECURITY.md) | [한국어](../ko/SECURITY.md)

# 安全政策

## 报告安全漏洞

我们非常重视安全问题。如果您在 Director Simulator: Scene Builder 中发现了安全漏洞，请负责任地报告。

### 如何报告

**请不要为安全漏洞公开创建 GitHub issue。**

相反，请通过以下方式报告安全问题：

1. **电子邮件:** lai.vu@siduko.com (首选)
2. **GitHub 安全公告:** https://github.com/siduko/unity-sandbox-director/security/advisories/new

### 报告需包含的内容

请提供：
- 漏洞描述
- 重现步骤
- 潜在影响
- 建议的修复方案（如果有）

### 响应时间表

- **初步响应:** 48 小时内
- **状态更新:** 7 天内
- **修复时间表:** 取决于严重程度（优先处理严重问题）

### 披露政策

- 我们将在 48 小时内确认收到您的报告
- 我们将与您合作以了解并修复问题
- 我们将在安全公告中鸣谢您（除非您希望保持匿名）
- 我们要求您在我们发布修复程序之前，不要公开披露该漏洞

## 支持的版本

| 版本 | 支持状态 |
| ------- | ------------------ |
| 0.1.x   | :white_check_mark: |
| < 0.1   | :x:                |

## 安全最佳实践

使用 Director Simulator 时：
- 保持 Unity 和依赖项更新
- 不要将敏感数据（API 密钥、令牌）提交到存储库
- 导入前检查第三方资产
- 使用 `.gitignore` 排除构建工件和本地配置文件

## 已知问题

目前没有已知问题。

---

感谢您帮助保护 Director Simulator 和我们社区的安全！ 🎬🔒