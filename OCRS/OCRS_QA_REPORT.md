# OCRS QA Report

Ngày kiểm tra: 20/10/2026 (Asia/Bangkok)  
Phạm vi: quản lý lớp Admin, đăng ký/hủy lớp, đồng bộ sĩ số, trang chi tiết khóa/lớp và UI liên quan.

## 1. Lỗi và nguyên nhân thực tế

1. **Trang chi tiết lớp luôn hiện nút đăng ký sau khi học viên đã đăng ký.** `ClassesController.Details` chỉ tải `CourseClass`, không truy vấn enrollment của user hiện tại; View vì thế không thể phân biệt `Approved`, `Pending`, `Cancelled` hay `Rejected`.
2. **Form Admin lưu lớp thiếu phản hồi và Edit có thể lỗi concurrency.** Form bind trực tiếp `CourseClass` (gồm navigation và `RowVersion`) trong khi không post concurrency token; action dùng `db.Update` trên entity rời với `RowVersion` rỗng, không bắt `DbUpdateException`/`DbUpdateConcurrencyException`, không đặt TempData thành công.
3. **Hủy đồng thời có thể hoàn chỗ nhiều lần.** Hai request có thể cùng đọc enrollment đang hoạt động trước khi cùng giảm `CurrentEnrollment`. Việc kiểm tra trạng thái và cập nhật trước đây không nguyên tử.
4. **Trang khóa học chỉ tải lớp `OpenForRegistration`.** Người dùng không thể xem trạng thái sắp mở/đã đóng khi chọn “Tất cả lớp”, và không có dữ liệu enrollment cá nhân trên từng card.
5. **UI lớp đang mở chưa đủ nổi bật.** Danh sách dạng hàng thiếu giảng viên/lịch/địa điểm/progress/status cá nhân và bộ lọc thực thi.
6. **Toast đặt giữa phía trên**, có thể che tiêu đề/hero.

## 2. Giải pháp đã áp dụng

- Tạo `ClassFormViewModel`: validation theo trường, ngày, sĩ số, hình thức học, địa điểm/link; xác minh `CourseId`, `InstructorId`, mã lớp trùng.
- Admin Create/Edit cập nhật entity đang tracking, giữ `CurrentEnrollment`, post/kiểm tra `RowVersion`, bắt và log lỗi DB/concurrency, giữ dữ liệu/dropdown khi lỗi, thông báo `Lưu lớp học thành công` chỉ sau `SaveChangesAsync`.
- Nút submit có loading state và bị khóa sau submit hợp lệ; server vẫn chống tạo trùng bằng unique index/mã lớp.
- Chi tiết lớp truy vấn enrollment mới nhất từ SQL Server; hiển thị đúng `Đã đăng ký`, `Đang chờ duyệt`, `Đăng ký lại`, hết chỗ/chưa mở/đã đóng và link `Xem lớp học của tôi`.
- Chi tiết khóa học tải mọi lớp, ưu tiên lớp đang mở/còn chỗ, kèm card, progress, trạng thái tuyển sinh/trạng thái cá nhân và bộ lọc thực tế.
- Hủy enrollment dùng conditional `ExecuteUpdateAsync` nguyên tử trước khi hoàn chỗ; chỉ request thắng mới giảm sĩ số.
- Toast chuyển sang góc trên bên phải; layout responsive và hỗ trợ `prefers-reduced-motion`.
- Chi tiết lớp bổ sung breadcrumb, registration card theo trạng thái và các lớp khác cùng khóa học.

## 3. File nguồn đã chỉnh sửa/tạo

- `Areas/Admin/Controllers/ClassesController.cs`
- `Areas/Admin/Views/Classes/Form.cshtml`
- `Controllers/ClassesController.cs`
- `Controllers/CoursesController.cs`
- `Services/CourseService.cs`
- `Services/RegistrationService.cs`
- `ViewModels/Admin/ClassFormViewModel.cs`
- `ViewModels/Catalog/ClassPresentationViewModels.cs`
- `Views/Courses/Details.cshtml`
- `Views/Classes/Details.cshtml`
- `wwwroot/js/site.js`
- `wwwroot/css/site.css`
- `../OCRS.Tests/OCRS.Tests.csproj`
- `../OCRS.Tests/ClassFormViewModelTests.cs`
- `../OCRS.Tests/SqlServerRegistrationTests.cs`
- `../OCRS.Tests/Usings.cs`

Các thay đổi SOA/API/Notification đã tồn tại trong working tree trước đợt sửa này được giữ nguyên.

## 4. Kết quả build và runtime

- `dotnet build --no-restore`: **PASS**, 0 warning, 0 error.
- HTTP smoke test: **PASS (200)** cho `/`, `/Courses`, `/Courses/Details/1`, `/Classes/Details/1`, `/Account/Login`.
- Đăng nhập demo Student: **PASS**; lớp đã ghi danh hiển thị `Đã đăng ký`, có link `Xem lớp học của tôi`, không còn form đăng ký hoạt động.
- Đăng nhập demo Admin: **PASS**; form tạo lớp trả 200, có anti-forgery token và client validation metadata.

## 5. Kết quả automated test

Lệnh: `dotnet test ../OCRS.Tests/OCRS.Tests.csproj --no-restore`  
Kết quả: **9/9 PASS**, 0 Failed, 0 Skipped.

| Nhóm | Kịch bản | Trạng thái |
|---|---|---|
| Admin | Tạo và sửa lớp qua `ClassesController`, xác minh dữ liệu SQL Server | PASS |
| Admin | Sĩ số 0, ngày kết thúc trước ngày bắt đầu, yêu cầu link Online | PASS |
| Registration | Đăng ký hợp lệ, trùng, lớp đầy, hủy và hủy lặp | PASS |
| Registration | Lớp cần duyệt lưu enrollment `Pending` | PASS |
| Concurrency | 500 học viên cạnh tranh 30 chỗ | PASS |
| Concurrency | Nhiều request cùng user không tạo enrollment hoạt động trùng | PASS |
| Concurrency | Nhiều request hủy chỉ hoàn đúng một chỗ | PASS |

## 6. Kiểm thử database và đồng thời

- Dùng SQL Server thật tại `localhost`, không dùng EF Core InMemory.
- Migration: database ứng dụng đã đồng bộ; không có migration chờ áp dụng.
- Mỗi test dùng database tên ngẫu nhiên `OCRS_Test_*`/`OCRS_Concurrency_*` và xóa sau test; kiểm tra cuối không còn database test.
- Kết quả tải đồng thời: **500 request, 30 chỗ, 30 thành công, `CurrentEnrollment = 30`, active enrollment = 30**, thời gian ghi nhận gần nhất 3.873 giây.
- Unique filtered index `(StudentId, ClassId)` cho `Pending/Approved` tiếp tục là lớp bảo vệ ở database.

## 7. Ma trận yêu cầu chính

| ID | Trạng thái | Ghi chú |
|---|---|---|
| ADM-01, ADM-09 | PASS | Create/Edit qua controller và SQL Server |
| ADM-02, ADM-03, ADM-04 | PASS (logic/runtime form) | Required/range và kiểm tra FK phía server; form giữ dữ liệu khi lỗi |
| ADM-05, ADM-06, ADM-07, ADM-08, ADM-10 | PASS | Validation, unique constraint, kiểm tra enrollment hiện tại |
| ADM-11, ADM-12 | PASS (code path) | Registration service bắt buộc status/time window hợp lệ |
| ADM-13 | PASS | Khóa nút client + unique code/concurrency phía server |
| ADM-14 | NOT RUN | Không chủ động làm gián đoạn SQL Server đang dùng |
| ADM-15 | PASS (code inspection) | Controller có `[Authorize(Roles="Admin")]`; chưa có E2E POST role khác |
| REG-01 | PASS (code inspection) | POST enrollment có authorize; chưa có browser E2E |
| REG-02 đến REG-10 | PASS | SQL integration + authenticated HTTP state |
| REG-11 | NOT RUN | Chưa tự động hóa thao tác Admin duyệt qua browser |
| REG-12 đến REG-14 | PASS | Hủy nguyên tử, đăng ký lại được phép, kiểm tra owner |
| REG-15 | PASS | 500 request/30 chỗ trên SQL Server |

## 8. Regression và UI

- Authentication login và phân quyền Student/Admin phục vụ luồng kiểm thử: **PASS**.
- Course list/detail, class detail và My Courses liên quan trực tiếp: **PASS (HTTP/runtime)**.
- Browser visual desktop/tablet/mobile, ảnh trước/sau: **NOT RUN** vì phiên Computer Use không có browser khả dụng.
- CRUD danh mục/khóa học, báo cáo, CRM, toàn bộ Instructor, đăng ký tài khoản/reset password: **NOT RUN** trong đợt ưu tiên này.
- Không phát hiện HTTP 500 hoặc exception trong các trang/luồng đã chạy.

## 9. Việc chưa xử lý/rủi ro còn lại

- Chưa có Playwright/browser runner trong dự án, nên chưa kiểm chứng pixel, breakpoint và JavaScript filter bằng E2E browser tự động.
- Chưa mô phỏng mất kết nối SQL Server (ADM-14) để tránh ảnh hưởng instance đang dùng; controller đã bắt `DbUpdateException` và log lỗi.
- Các module ngoài phạm vi ưu tiên nêu tại mục 8 chưa được tuyên bố hoạt động đầy đủ.
- `CurrentEnrollment` vẫn là cột denormalized có thẩm quyền cập nhật qua nghiệp vụ; các thay đổi enrollment ngoài service cần tiếp tục tuân thủ cùng transaction/conditional update.

## 10. Ảnh chụp

Không có: công cụ browser không khả dụng, vì vậy không tạo bằng chứng ảnh giả.
