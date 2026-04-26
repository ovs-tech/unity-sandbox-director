[English](../../SECURITY.md) | [Tiếng Việt](../vi/SECURITY.md) | [中文](../zh/SECURITY.md) | [日本語](../ja/SECURITY.md) | [한국어](../ko/SECURITY.md)

# Security Policy

## Reporting a Vulnerability

We take security seriously. If you discover a security vulnerability in Director Simulator: Scene Builder, please report it responsibly.

### How to Report

**Please do NOT open a public GitHub issue for security vulnerabilities.**

Instead, report security issues via:

1. **Email:** lai.vu@siduko.com (preferred)
2. **GitHub Security Advisory:** https://github.com/siduko/unity-sandbox-director/security/advisories/new

### What to Include

Please provide:
- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if you have one)

### Response Timeline

- **Initial Response:** Within 48 hours
- **Status Update:** Within 7 days
- **Fix Timeline:** Depends on severity (critical issues prioritized)

### Disclosure Policy

- We will acknowledge your report within 48 hours
- We will work with you to understand and fix the issue
- We will credit you in the security advisory (unless you prefer to remain anonymous)
- We ask that you do not publicly disclose the vulnerability until we have released a fix

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 0.1.x   | :white_check_mark: |
| < 0.1   | :x:                |

## Security Best Practices

When using Director Simulator:
- Keep Unity and dependencies up to date
- Do not commit sensitive data (API keys, tokens) to the repository
- Review third-party assets before importing
- Use `.gitignore` to exclude build artifacts and local config files

## Known Issues

None at this time.

---

Thank you for helping keep Director Simulator and our community safe! 🎬🔒