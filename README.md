# LangCenter – Trung tâm ngoại ngữ

LangCenter là API quản lý lớp, ghi danh học viên và thu học phí cho trung tâm ngoại ngữ. Hệ thống kiểm tra sĩ số trước khi khai giảng, kiểm tra công nợ trước khi xác nhận dự thi cuối khóa và hỗ trợ tra cứu chứng chỉ qua mã QR.

Dự án dùng C# .NET 10, ASP.NET Core Web API với Controllers, Swagger, EF Core và PostgreSQL.

## Tài liệu

- [Yêu cầu và quy tắc nghiệp vụ](docs/requirements.md)
- [Kiến trúc, mô hình dữ liệu và hợp đồng API](docs/design.md)

## Chạy dự án

### Docker Compose

Mở thư mục gốc của repo và chạy bằng Docker với Linux containers.

```bash
docker compose up --build
```

- Swagger [http://localhost:8080/swagger](http://localhost:8080/swagger)
- Trạng thái API [http://localhost:8080/healthz](http://localhost:8080/healthz)

API lắng nghe trên localhost. PostgreSQL chạy trong mạng Compose và dùng thông tin kết nối dành cho môi trường phát triển tại máy. Chạy `docker compose down` để dừng các service.

### Chạy trực tiếp bằng .NET 10

```bash
dotnet run --project LangCenter.API --launch-profile http
```

Mở [Swagger ở cổng 5080](http://localhost:5080/swagger). Ở mốc khung dự án, Swagger và `/healthz` có thể chạy trước khi kết nối cơ sở dữ liệu. Cấu hình biến môi trường `ConnectionStrings__DefaultConnection` khi cần truy cập PostgreSQL từ API chạy trực tiếp.

### Đăng nhập trong Swagger

Đặt mật khẩu demo trước khi khởi động Docker Compose. Trong PowerShell 7, có thể nhập mật khẩu mà không hiển thị bằng lệnh sau.

```powershell
$env:LANGCENTER_DEMO_PASSWORD = Read-Host 'Mật khẩu demo' -MaskInput
docker compose up -d --build --wait
```

Mật khẩu có ít nhất 12 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt. Lần đầu API tạo ba tài khoản ở môi trường Development và lưu mật khẩu dạng băm trong PostgreSQL.

| Vai trò | Email demo |
| --- | --- |
| Admin | `admin@langcenter.local` |
| Registrar | `registrar@langcenter.local` |
| Cashier | `cashier@langcenter.local` |

Gọi `POST /api/auth/login` với email và mật khẩu vừa đặt, sao chép `accessToken` rồi dán vào nút **Authorize** trên Swagger. Gọi `GET /api/auth/me` để xem vai trò. Token mặc định có hiệu lực 30 phút. Admin và Registrar có thể tạo và xem học viên qua `/api/students`.

Docker Compose tự áp dụng migration cho môi trường Development. Khi chạy API ngoài Docker, dùng `dotnet tool restore` rồi áp dụng migration bằng lệnh sau sau khi cấu hình `ConnectionStrings__DefaultConnection`.

```bash
dotnet ef database update --project LangCenter.Infrastructure --startup-project LangCenter.API
```

Khi chưa đặt khóa JWT, Development tạo khóa mới mỗi lần khởi động. Dùng biến môi trường `JwtSettings__SigningKey` cho API hoặc `LANGCENTER_JWT_SIGNING_KEY` cho Docker Compose để đặt khóa ổn định. Production yêu cầu khóa ngẫu nhiên ít nhất 32 byte mã hóa Base64.

## Kiểm thử và CI

```bash
dotnet test LangCenter.sln --configuration Release
```

[Test tích hợp](LangCenter.Tests/Integration/) chạy trên host kiểm thử ASP.NET Core. Bộ test kiểm tra khởi động API, Swagger, lỗi `ProblemDetails`, đăng nhập, phân quyền và lưu học viên. Các ca đăng nhập và học viên dùng SQLite trong bộ nhớ để chạy độc lập.

[Workflow CI](.github/workflows/ci.yml) chạy restore, build và test khi push lên `main` hoặc mở Pull Request vào `main`.

## Phân công

| Thành viên | Commit dự kiến | Phần việc |
| --- | --- | --- |
| Trần Hồng Quang | 1–18 | Phân tích, thiết kế, nền dự án, xác thực, phân quyền và quản lý học viên |
| Võ Văn Bảo Quốc | 19–28 | Quản lý lớp, ghi danh, khai giảng đủ sĩ số và kiểm thử |
| Phạm Quốc Thanh | 29–38 | Học phí, điều kiện dự thi, chức năng mở rộng và kiểm thử |
| Huỳnh Quốc Thịnh | 39–48 | Chứng chỉ QR, độ bao phủ, dữ liệu demo, README và minh chứng |

Các khoảng số là mục trong kế hoạch 48 công việc. Commit bổ sung để cập nhật danh sách thành viên được tính riêng.

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
