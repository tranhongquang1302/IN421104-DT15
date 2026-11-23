# Yêu cầu LangCenter

## Bài toán

Trung tâm cần theo dõi lớp dự kiến, học viên ghi danh và tiền học phí. Nhân viên giáo vụ chỉ khai giảng lớp đủ sĩ số. Hệ thống kiểm tra học phí của lượt ghi danh trước khi cho phép dự thi cuối khóa. Chứng chỉ đã cấp có mã QR để kiểm tra thông tin.

## Vai trò và quyền

| Vai trò | Quyền nghiệp vụ |
| --- | --- |
| Quản trị viên (`Admin`) | Quản lý tài khoản và có toàn quyền thao tác nghiệp vụ |
| Giáo vụ (`Registrar`) | Quản lý học viên, tạo và xem lớp, ghi danh, khai giảng, xác nhận dự thi và cấp chứng chỉ |
| Thu ngân (`Cashier`) | Ghi nhận thanh toán, xem số dư học phí và xem báo cáo công nợ nếu chức năng này được nhóm chọn |
| Người tra cứu | Xem thông tin xác minh chứng chỉ bằng mã tra cứu công khai |

Tài khoản đăng nhập là tài khoản nhân viên. Học viên được lưu thành hồ sơ phục vụ ghi danh. Trường phân quyền hiện thực theo ma trận API trong tài liệu thiết kế.

## Phạm vi cốt lõi

- Đăng nhập và kiểm tra quyền theo vai trò.
- Tạo và xem lớp với số lượng ghi danh tối thiểu và mức học phí của lớp.
- Quản lý hồ sơ học viên và ghi danh vào lớp đang nhận học viên.
- Khai giảng, ghi nhận các khoản thanh toán và xác nhận dự thi cuối khóa.
- Cấp chứng chỉ và tra cứu chứng chỉ qua mã QR.

Luồng demo dùng Web API và Swagger. Đơn vị học phí là đồng Việt Nam, lưu dưới dạng số nguyên đồng. Mức học phí được ghi nhận trên lượt ghi danh để xác định số phải thu của học viên tại thời điểm đó.

## Chức năng mở rộng

| Phương án làm việc | Hành vi dự kiến | Phụ trách |
| --- | --- | --- |
| Báo cáo công nợ học phí theo lớp | Thu ngân hoặc quản trị viên xem số phải thu, số đã thu và số còn nợ theo từng lượt ghi danh và tổng của lớp | Phạm Quốc Thanh |

Đây là phương án dùng để lập kế hoạch cho commit 35–36. Nhóm xác nhận lựa chọn trước khi triển khai hai commit này.

## Quy tắc nghiệp vụ

| Mã | Quy tắc | Tiêu chí kiểm tra |
| --- | --- | --- |
| QT01 | Lớp chỉ được khai giảng khi đang nhận ghi danh và sĩ số đạt ngưỡng tối thiểu của lớp. | Dưới ngưỡng trả lỗi; bằng hoặc vượt ngưỡng cho phép khai giảng. |
| QT02 | Mỗi học viên chỉ có một lượt ghi danh cho cùng một lớp. | Yêu cầu lặp bị từ chối và cơ sở dữ liệu vẫn có một bản ghi. |
| QT03 | Ghi danh chỉ thực hiện khi lớp đang nhận học viên. | Lớp đã khai giảng từ chối ghi danh mới. |
| QT04 | Học phí đã thu là tổng các khoản thanh toán của lượt ghi danh. Mỗi khoản là số nguyên dương và tối đa bằng số còn nợ tại thời điểm ghi nhận. | Đóng một phần giảm số dư; đóng đủ đưa số dư về 0; số 0, số âm và số vượt công nợ bị từ chối. |
| QT05 | Giáo vụ chỉ xác nhận cho học viên dự thi cuối khóa khi lớp đã khai giảng và lượt ghi danh có số dư học phí bằng 0. | Còn nợ thì bị từ chối và không có bản ghi xác nhận; đóng đủ thì được xác nhận nếu lớp đã khai giảng. |
| QT06 | Một lượt ghi danh có tối đa một chứng chỉ. Giáo vụ xác nhận học viên đã hoàn thành khóa trước khi cấp và hệ thống yêu cầu bản ghi xác nhận dự thi. | Cấp thành công tạo mã tra cứu; yêu cầu lặp bị từ chối; mã không tồn tại trả 404. |

QR mở URL tra cứu bằng mã riêng của chứng chỉ. Phản hồi công khai gồm họ tên người được cấp, tên lớp và ngày cấp. Dữ liệu đăng nhập, liên lạc và học phí chỉ phục vụ nghiệp vụ nội bộ.

## Luồng nghiệp vụ

1. Quản trị viên hoặc giáo vụ tạo học viên và lớp, rồi ghi danh học viên.
2. Giáo vụ yêu cầu khai giảng. Hệ thống đếm số lượt ghi danh của lớp và áp dụng QT01.
3. Thu ngân ghi nhận thanh toán. Giáo vụ xác nhận dự thi khi thỏa QT05.
4. Sau khi giáo vụ xác nhận học viên hoàn thành khóa, nhân viên có quyền cấp chứng chỉ và cung cấp mã QR.
5. Người tra cứu mở URL từ QR và nhận thông tin xác minh chứng chỉ.
