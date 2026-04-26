[English](../../SECURITY.md) | [Tiếng Việt](../vi/SECURITY.md) | [中文](../zh/SECURITY.md) | [日本語](../ja/SECURITY.md) | [한국어](../ko/SECURITY.md)

# Chính sách Bảo mật

## Báo cáo Lỗ hổng Bảo mật

Chúng tôi rất coi trọng vấn đề bảo mật. Nếu bạn phát hiện ra lỗ hổng bảo mật trong Director Simulator: Scene Builder, vui lòng báo cáo một cách có trách nhiệm.

### Cách Báo cáo

**Vui lòng KHÔNG mở GitHub issue công khai cho các lỗ hổng bảo mật.**

Thay vào đó, hãy báo cáo các vấn đề bảo mật qua:

1. **Email:** lai.vu@siduko.com (ưu tiên)
2. **GitHub Security Advisory:** https://github.com/siduko/unity-sandbox-director/security/advisories/new

### Nội dung cần Cung cấp

Vui lòng cung cấp:
- Mô tả về lỗ hổng
- Các bước để tái hiện
- Tác động tiềm ẩn
- Đề xuất khắc phục (nếu có)

### Thời gian Phản hồi

- **Phản hồi ban đầu:** Trong vòng 48 giờ
- **Cập nhật Trạng thái:** Trong vòng 7 ngày
- **Thời gian Khắc phục:** Phụ thuộc vào mức độ nghiêm trọng (ưu tiên các vấn đề nghiêm trọng)

### Chính sách Tiết lộ

- Chúng tôi sẽ xác nhận báo cáo của bạn trong vòng 48 giờ
- Chúng tôi sẽ làm việc với bạn để hiểu và khắc phục sự cố
- Chúng tôi sẽ ghi nhận đóng góp của bạn trong thông báo bảo mật (trừ khi bạn muốn ẩn danh)
- Chúng tôi yêu cầu bạn không công bố lỗ hổng bảo mật cho đến khi chúng tôi đã phát hành bản vá

## Các phiên bản được Hỗ trợ

| Phiên bản | Được Hỗ trợ |
| ------- | ------------------ |
| 0.1.x   | :white_check_mark: |
| < 0.1   | :x:                |

## Các Biện pháp Bảo mật Tốt nhất

Khi sử dụng Director Simulator:
- Cập nhật Unity và các thư viện phụ thuộc
- Không commit dữ liệu nhạy cảm (API keys, tokens) vào kho lưu trữ
- Kiểm tra các assets của bên thứ ba trước khi import
- Sử dụng `.gitignore` để loại trừ các tệp build và cấu hình cục bộ

## Các sự cố Đã biết

Chưa có thông tin tại thời điểm này.

---

Cảm ơn bạn đã giúp giữ cho Director Simulator và cộng đồng của chúng ta an toàn! 🎬🔒