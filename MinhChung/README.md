# Minh chứng LangCenter

Ảnh dùng PNG và đặt tên theo bảng. Chụp khi nhóm đang chạy phiên bản nộp bài và che mật khẩu, access token cùng thông tin học viên thật.

| Tệp | Nội dung |
| --- | --- |
| `01-swagger.png` | Giao diện Swagger hiển thị các nhóm API |
| `02-dang-nhap-admin.png` | Kết quả kiểm tra vai trò Admin qua `GET /api/auth/me` |
| `03-kiem-thu.png` | Kết quả `dotnet test` |
| `04-do-bao-phu.png` | Tỷ lệ dòng và nhánh từ Cobertura hoặc Summary của CI |
| `05-github-actions.png` | GitHub Actions chạy thành công trên phiên bản nộp bài |
| `06-pull-requests.png` | Danh sách Pull Request của nhóm |
| `07-contributors.png` | Biểu đồ **Insights → Contributors** |
| `08-bang-cong-viec.png` | GitHub Issues hoặc Projects thể hiện phân công và tiến độ |
| `09-tra-cuu-chung-chi.png` | Kết quả tra cứu công khai bằng mã QR |

Thêm ảnh hoặc biên bản họp nếu nhóm có buổi họp cần ghi nhận.

## Video demo

Quay màn hình bằng ScreenPal trong 5–10 phút theo thứ tự sau.

1. Chạy `docker compose up` và mở Swagger.
2. Đăng nhập Admin, Registrar và Cashier. Gọi `GET /api/auth/me` để xác nhận từng vai trò.
3. Demo tạo lớp và học viên, ghi danh, khai giảng, thu học phí và xem công nợ. Có thể chuẩn bị lượt ghi danh cho phần sau bằng script trong README gốc.
4. Xác nhận dự thi cho lượt đã đóng đủ. Cấp chứng chỉ, tải ảnh QR và mở link tra cứu trong cửa sổ ẩn danh.
5. Thử xác nhận dự thi cho học viên còn nợ và hiển thị lỗi `400`. Có thể dùng `PartialEnrollmentId` từ script demo.
6. Chạy kiểm thử và hiển thị kết quả.

Dán duy nhất URL chia sẻ ScreenPal vào `link-video.txt`. Mở bằng cửa sổ ẩn danh để kiểm tra giảng viên xem được mà không cần đăng nhập.
