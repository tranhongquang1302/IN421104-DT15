# LangCenter – Trung tâm ngoại ngữ

LangCenter là API quản lý lớp, ghi danh học viên và thu học phí cho trung tâm ngoại ngữ. Hệ thống kiểm tra sĩ số trước khi khai giảng, kiểm tra công nợ trước khi xác nhận dự thi cuối khóa và hỗ trợ tra cứu chứng chỉ qua mã QR.

Dự án dùng C# .NET 10, ASP.NET Core Web API với Controllers, Swagger, EF Core và PostgreSQL.

## Tài liệu

- [Yêu cầu và quy tắc nghiệp vụ](docs/requirements.md)
- [Kiến trúc, mô hình dữ liệu và hợp đồng API](docs/design.md)

## Phân công

| Thành viên | Commit dự kiến | Phần việc |
| --- | --- | --- |
| Trần Hồng Quang | 1–12 | Phân tích, thiết kế, khung solution, PostgreSQL, Swagger, kiểm thử nền, CI và Docker Compose |
| A | 13–24 | Đăng nhập, phân quyền, học viên và tạo lớp |
| B | 25–36 | Ghi danh, khai giảng, học phí, dự thi, chức năng mở rộng và kiểm thử |
| C | 37–48 | Chứng chỉ QR, độ bao phủ, dữ liệu demo, README và minh chứng |

Nhóm cập nhật tên A, B, C khi phân công trên GitHub. Tài khoản mẫu cho từng vai trò được bổ sung khi có dữ liệu demo.

## Quy trình làm việc

1. Nhận GitHub Issue có phạm vi, quy tắc liên quan và tiêu chí hoàn thành.
2. Tạo nhánh từ phiên bản mới nhất của `main`. Dùng tên tiếng Anh như `feat/class-launch`.
3. Viết code và kiểm thử theo phần việc. Dùng tên tiếng Anh cho lớp, phương thức, biến, bảng, cột và ca kiểm thử.
4. Commit theo `type(scope): subject`. Dùng câu mệnh lệnh ngắn bằng tiếng Anh, ví dụ `feat(classes): enforce minimum enrollment at launch`.
5. Mở Pull Request, liên kết Issue và ghi cách kiểm tra. Giữ các commit cùng tác giả khi tích hợp theo thứ tự bàn giao.

| Loại commit | Dùng khi |
| --- | --- |
| `feat` | Thêm tính năng |
| `fix` | Sửa lỗi |
| `docs` | Sửa tài liệu hoặc minh chứng |
| `refactor` | Sắp xếp code và giữ hành vi |
| `test` | Thêm hoặc sửa kiểm thử |
| `chore` | Thay đổi cấu hình hoặc công việc bảo trì |
