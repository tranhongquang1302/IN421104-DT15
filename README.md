# LangCenter – Trung tâm ngoại ngữ

[![CI](https://github.com/tranhongquang1302/IN421104-DT15/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/tranhongquang1302/IN421104-DT15/actions/workflows/ci.yml)

LangCenter là ứng dụng web quản lý lớp, ghi danh học viên và thu học phí cho trung tâm ngoại ngữ. Hệ thống kiểm tra sĩ số trước khi khai giảng, kiểm tra công nợ trước khi xác nhận dự thi cuối khóa và hỗ trợ tra cứu chứng chỉ qua mã QR.

Dự án dùng C# .NET 10, Razor Pages, ASP.NET Core Controllers, Swagger, EF Core và PostgreSQL.

## Tài liệu

- [Yêu cầu và quy tắc nghiệp vụ](docs/requirements.md)
- [Kiến trúc, mô hình dữ liệu và hợp đồng API](docs/design.md)
- [Danh mục minh chứng nộp bài](MinhChung/README.md)

## Chạy dự án

### Docker Compose

Mở thư mục gốc của repo và chạy bằng Docker với Linux containers. Đặt mật khẩu theo mục [Sử dụng giao diện web](#sử-dụng-giao-diện-web) trước khi chạy lần đầu để API tạo tài khoản demo.

```bash
docker compose up --build
```

- Giao diện LangCenter [http://localhost:8080/](http://localhost:8080/)
- Swagger [http://localhost:8080/swagger](http://localhost:8080/swagger)
- Trạng thái API [http://localhost:8080/healthz](http://localhost:8080/healthz)

API lắng nghe trên localhost theo mặc định. PostgreSQL chạy trong mạng Compose và dùng thông tin kết nối dành cho môi trường phát triển tại máy. Lệnh `docker compose up -d --build --wait` đợi API trả `200` tại `/healthz`. Chạy `docker compose down` để dừng các service.

### Chạy trực tiếp bằng .NET 10

```bash
dotnet run --project LangCenter.API --launch-profile http
```

Mở [Swagger ở cổng 5080](http://localhost:5080/swagger). Đặt `ConnectionStrings__DefaultConnection` để API chạy trực tiếp kết nối PostgreSQL. Đặt `CertificateLinks__PublicBaseUrl` nếu địa chỉ truy cập từ ngoài khác địa chỉ mặc định `http://localhost:5080`.

### Sử dụng giao diện web

Đặt mật khẩu demo trước khi khởi động Docker Compose. Trong PowerShell 7, có thể nhập mật khẩu mà không hiển thị bằng lệnh sau.

```powershell
$env:LANGCENTER_DEMO_PASSWORD = Read-Host 'Mật khẩu demo' -MaskInput
docker compose up -d --build --wait
```

Mật khẩu có ít nhất 12 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt. Lần đầu API tạo ba tài khoản ở môi trường Development và lưu mật khẩu dạng băm trong PostgreSQL. Dùng lại mật khẩu đã đặt nếu chạy với volume PostgreSQL cũ. Thay đổi biến môi trường chỉ ảnh hưởng tài khoản được tạo mới.

| Vai trò | Email demo |
| --- | --- |
| Admin | `admin@langcenter.local` |
| Registrar | `registrar@langcenter.local` |
| Cashier | `cashier@langcenter.local` |

Mở [LangCenter](http://localhost:8080/) và đăng nhập bằng email theo vai trò cùng mật khẩu demo. Giáo vụ vào **Lớp học** hoặc **Học viên** để tạo hồ sơ, ghi danh, khai giảng, xác nhận dự thi và cấp chứng chỉ. Thu ngân vào **Học phí và công nợ** để tra cứu theo email và làm việc theo từng lớp. Admin có cả hai nhóm chức năng. Trang [tra cứu chứng chỉ](http://localhost:8080/certificates/verify) dùng được khi chưa đăng nhập.

### Đăng nhập trong Swagger

Gọi `POST /api/auth/login` với email và mật khẩu vừa đặt, sao chép `accessToken` rồi dán vào nút **Authorize** trên Swagger. Gọi `GET /api/auth/me` để xem vai trò. Token mặc định có hiệu lực 30 phút. Admin và Registrar có thể tạo và xem học viên qua `/api/students`.

Docker Compose tự áp dụng migration cho môi trường Development. Khi chạy API ngoài Docker, dùng `dotnet tool restore` rồi áp dụng migration bằng lệnh sau sau khi cấu hình `ConnectionStrings__DefaultConnection`.

```bash
dotnet ef database update --project LangCenter.Infrastructure --startup-project LangCenter.API
```

Khi nâng cấp một cơ sở dữ liệu có sẵn, migration `RequireUniqueStudentEmail` sẽ dừng nếu có hồ sơ trùng email. Kiểm tra bằng câu lệnh chỉ đọc dưới đây, đối chiếu hồ sơ với các lượt ghi danh và chỉ sửa dữ liệu sau khi xác nhận thông tin chính xác. Sau đó chạy lại migration.

```sql
SELECT "Email", COUNT(*) FROM students GROUP BY "Email" HAVING COUNT(*) > 1;
```

Khi chưa đặt khóa JWT, Development tạo khóa mới mỗi lần khởi động. Dùng biến môi trường `JwtSettings__SigningKey` cho API hoặc `LANGCENTER_JWT_SIGNING_KEY` cho Docker Compose để đặt khóa ổn định. Production yêu cầu khóa ngẫu nhiên ít nhất 32 byte mã hóa Base64.

### Tạo lớp, ghi danh và khai giảng

1. Đăng nhập bằng tài khoản Admin hoặc Registrar và bật **Authorize**.
2. Gọi `POST /api/classes` với `name`, `minimumEnrollment` và `tuitionFee`. Dùng ID từ kết quả để xem chi tiết tại `GET /api/classes/{id}`.
3. Tạo học viên bằng `POST /api/students`. Mỗi email dùng cho một hồ sơ học viên. Email đã dùng trả `409 Conflict`, kể cả khi khác chữ hoa hoặc khoảng trắng đầu cuối. Gọi `POST /api/classes/{classId}/enrollments` với `studentId`. Kết quả ghi lại số tiền phải thu ở trường `tuitionDue`.
4. Gọi `POST /api/classes/{id}/launch`. Lớp dưới ngưỡng trả `400`; lớp vừa đủ hoặc vượt ngưỡng chuyển sang `Started`. Ghi danh mới sau khi khai giảng cũng trả `400`.

### Thu học phí, xác nhận dự thi và xem công nợ

1. Đăng nhập bằng tài khoản Cashier hoặc Admin. Gọi `GET /api/enrollments?studentEmail={email}` bằng email học viên và lấy `enrollmentId` của lớp cần thu.
2. Gọi `POST /api/enrollments/{id}/payments` với `amount` là số nguyên đồng dương. Có thể gửi `requestId` là một UUID riêng cho giao dịch. Nếu cần gửi lại giao dịch, dùng cùng mã và số tiền để nhận lại khoản thu cũ.
3. Gọi `GET /api/enrollments/{id}/balance` để xem công nợ. Khoản thu vượt số còn nợ trả `400`.
4. Đăng nhập bằng tài khoản Registrar hoặc Admin. Gọi `POST /api/enrollments/{id}/exam-admission`. Lớp phải ở trạng thái `Started` và dư nợ phải bằng 0. Khi học viên còn nợ, API trả `400` kèm `ProblemDetails`.
5. Đăng nhập bằng tài khoản Cashier hoặc Admin. Gọi `GET /api/reports/classes/{id}/tuition` để xem số phải thu, đã thu và còn nợ của lớp cùng chi tiết từng lượt ghi danh.

### Cấp chứng chỉ và tra cứu qua QR

1. Dùng lớp và học viên đã tạo trong các bước trên hoặc chạy `pwsh -File scripts/Seed-Demo.ps1` sau khi Docker Compose sẵn sàng. Script tạo một lớp có hai lượt ghi danh. `PartialEnrollmentId` còn nợ 600 đồng và `EligibleEnrollmentId` đã đóng đủ, có xác nhận đủ điều kiện dự thi.
2. Đăng nhập bằng Registrar hoặc Admin. Gọi `POST /api/enrollments/{id}/certificate` cho `EligibleEnrollmentId` với `{"completionConfirmed": true}`. API trả `201` cùng `id` chứng chỉ và `verifyUrl`. Lượt ghi danh thiếu xác nhận dự thi hoặc xác nhận hoàn thành trả `400`. Cấp lần hai trả `409`.
3. Gọi `GET /api/certificates/{id}/qr` với **ID chứng chỉ** và tải ảnh PNG trong Swagger. Quét ảnh QR để mở trang tra cứu chứng chỉ có họ tên học viên, lớp và ngày cấp. Trường `verifyUrl` trong phản hồi API vẫn mở kết quả JSON. Mã không hợp lệ trả `404`.

URL mặc định của Compose dùng `localhost:8080`. Khi quét bằng điện thoại, dùng Wi-Fi chung với máy chạy Docker và đổi địa chỉ bên dưới thành IP LAN của máy. Dùng dữ liệu demo trong mạng riêng.

```powershell
$env:LANGCENTER_BIND_ADDRESS = '192.168.1.20'
$env:LANGCENTER_CERTIFICATE_BASE_URL = "http://$($env:LANGCENTER_BIND_ADDRESS):8080"
docker compose up -d --build --wait
```

Mở đường dẫn tra cứu trên điện thoại để kiểm tra kết nối trước khi quay. URL trong ảnh QR đã tải giữ địa chỉ từ lúc tạo ảnh. Tải ảnh mới sau khi đổi cấu hình.

## Kiểm thử và CI

```bash
dotnet test LangCenter.sln --configuration Release
```

[Test tích hợp](LangCenter.Tests/Integration/) chạy trên host kiểm thử ASP.NET Core. Bộ test kiểm tra khởi động API, Swagger, lỗi `ProblemDetails`, đăng nhập, phân quyền, lớp học, học viên, ghi danh, học phí, dự thi, báo cáo công nợ và chứng chỉ QR. Phần lớn ca tích hợp dùng SQLite trong bộ nhớ để chạy độc lập.

[Workflow CI](.github/workflows/ci.yml) chạy restore, build và test khi push lên `main` hoặc mở Pull Request vào `main`. Job `postgres-test` chạy các ca đồng thời về học phí và cấp chứng chỉ trên PostgreSQL 16.

Để chạy thêm các ca PostgreSQL tại máy, đặt biến môi trường `LANGCENTER_TEST_POSTGRES` thành chuỗi kết nối của PostgreSQL kiểm thử. Tài khoản cần quyền tạo cơ sở dữ liệu. Test tự tạo một cơ sở dữ liệu có tên ngẫu nhiên, áp dụng migration rồi xóa cơ sở dữ liệu đó sau khi chạy.

```bash
dotnet test LangCenter.sln --configuration Release --filter FullyQualifiedName~Postgres
```

### Đo độ bao phủ

```powershell
dotnet test LangCenter.sln --configuration Release --settings coverlet.runsettings --collect "XPlat Code Coverage" --logger "trx;LogFileName=langcenter.trx" --results-directory TestResults
$file = Get-ChildItem TestResults -Recurse -Filter coverage.cobertura.xml | Sort-Object LastWriteTime -Descending | Select-Object -First 1
[xml]$report = Get-Content -LiteralPath $file.FullName -Raw
"Lines {0:P1} ({1}/{2})" -f [double]$report.coverage.'line-rate', $report.coverage.'lines-covered', $report.coverage.'lines-valid'
"Branches {0:P1} ({1}/{2})" -f [double]$report.coverage.'branch-rate', $report.coverage.'branches-covered', $report.coverage.'branches-valid'
```

Báo cáo tính bốn tầng ứng dụng và bỏ qua mã migration sinh tự động. GitHub Actions hiển thị tỷ lệ ở **Summary** của lần chạy và lưu tệp TRX cùng Cobertura trong artifact `test-results`.

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
