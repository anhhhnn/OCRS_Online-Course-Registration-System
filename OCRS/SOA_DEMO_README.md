# OCRS – SOA Service Demonstration

## 1. Giới thiệu

OCRS vẫn là một ứng dụng ASP.NET Core MVC duy nhất. Phần SOA bổ sung các hợp đồng HTTP trên chính các nghiệp vụ hiện có để website MVC và client khác có thể tái sử dụng chúng. Đây là bản demo áp dụng nguyên lý hướng dịch vụ trong ứng dụng MVC, không phải hệ thống SOA doanh nghiệp phân tán hoàn chỉnh; project không triển khai ESB, service registry hay message broker.

Luồng minh họa:

```text
Service Consumer (MVC / browser client)
        → REST API (HTTP + JSON)
        → Service Provider (business services)
        → Entity Framework Core → SQL Server OCRS
```

## 2. Các dịch vụ

- `CourseService`: dữ liệu khóa học công khai, tìm kiếm và lọc; được dùng bởi MVC `CoursesController` và Course API.
- `ClassService`: chi tiết lớp, lịch học và chỗ còn lại. Chỗ trống hiển thị chỉ mang tính tham khảo.
- `RegistrationService`: đăng ký, tra cứu và hủy đăng ký. Service giữ transaction, atomic conditional update và unique filtered index để chống overbooking/trùng đăng ký.
- `UserService`: chỉ đọc/cập nhật hồ sơ của danh tính đang đăng nhập; không trả `PasswordHash`, stamp hoặc hồ sơ người khác.
- `NotificationService`: chỉ đọc/đánh dấu thông báo thuộc danh tính hiện tại; thông báo đăng ký được sinh nội bộ bởi `RegistrationService`.

## 3. Service contract

Các endpoint đọc Course/Class là công khai. Endpoint User/Notification cần Identity cookie. Endpoint Registration cần Identity cookie và role `Student`. Mọi endpoint ghi dùng anti-forgery token; trình duyệt cùng origin gửi token trong header `RequestVerificationToken`. API không nhận mật khẩu và không tự phát hành token demo thiếu an toàn.

| Method | URL | Input | Response chính | Auth | Status |
|---|---|---|---|---|---|
| GET | `/api/soa/courses` | `categoryId?`, `learningMode?` | `CourseDto[]` | Public | 200 |
| GET | `/api/soa/courses/{id}` | route `id` | `CourseDto` | Public | 200, 404 |
| GET | `/api/soa/courses/search` | `keyword` | `CourseDto[]` | Public | 200, 400 |
| GET | `/api/soa/courses/{courseId}/classes` | route `courseId` | `ClassDto[]` | Public | 200 |
| GET | `/api/soa/classes/{id}` | route `id` | `ClassDto` | Public | 200, 404 |
| GET | `/api/soa/classes/{id}/schedule` | route `id` | `ClassScheduleDto[]` | Public | 200, 404 |
| GET | `/api/soa/classes/{id}/availability` | route `id` | `ClassAvailabilityDto` | Public | 200, 404 |
| POST | `/api/soa/registrations` | `{ "classId": 9 }` | `RegistrationResultDto` | Student | 201, 400, 401, 403, 409 |
| GET | `/api/soa/registrations/my` | — | `EnrollmentDto[]` | Student | 200, 401, 403 |
| GET | `/api/soa/registrations/classes/{classId}/my-status` | route `classId` | `MyRegistrationStatusDto` | Student | 200, 401, 403 |
| POST | `/api/soa/registrations/{id}/cancel` | route `id` | `RegistrationResultDto` | Student | 200, 401, 403, 404, 409 |
| GET | `/api/soa/users/me` | — | `CurrentUserDto` | Signed in | 200, 401, 404 |
| PUT | `/api/soa/users/me` | `UpdateCurrentUserDto` | `CurrentUserDto` | Signed in | 200, 400, 401 |
| GET | `/api/soa/notifications/my` | — | `NotificationDto[]` | Signed in | 200, 401 |
| GET | `/api/soa/notifications/unread-count` | — | `{ "count": n }` | Signed in | 200, 401 |
| POST | `/api/soa/notifications/{id}/read` | route `id` | `ApiMessageDto` | Signed in | 200, 401, 404 |

Lỗi validation do `[ApiController]` trả Problem Details (400). Lỗi nghiệp vụ đăng ký trả `RegistrationResultDto` gồm `success`, `status`, `message`, `enrollmentId`, `errorCode`.

## 4. Nguyên lý SOA trong code

- **Loose Coupling:** controller/client phụ thuộc service contract và DTO, không truy cập database trực tiếp.
- **Reusability:** `CoursesController` MVC và `SoaCoursesController` cùng dùng `ICourseService`; client tĩnh tái sử dụng API.
- **Interoperability:** HTTP, status code và JSON là giao diện trung lập công nghệ.
- **Encapsulation:** query và quy tắc nghiệp vụ nằm trong `Services/`; entity EF không được trả trực tiếp qua API.
- **Composability:** đăng ký kết hợp xác định học viên, kiểm tra lớp, giữ chỗ, tạo enrollment và notification trong một transaction.

## 5. Chống overbooking

`RegistrationService.RegisterAsync` thực hiện conditional SQL update chỉ khi `CurrentEnrollment < MaxCapacity`, trong transaction SQL Server. Nếu insert Enrollment xung đột filtered unique index `(StudentId, ClassId)` cho trạng thái đang hiệu lực, transaction rollback nên phần tăng sĩ số cũng rollback. Check constraint ngăn `CurrentEnrollment` âm hoặc vượt `MaxCapacity`. Thành công chỉ được trả sau `CommitAsync`.

Hủy chỉ chấp nhận enrollment `Pending/Approved`, đổi trạng thái và giảm sĩ số trong transaction. Lần hủy lặp bị từ chối nên không giảm hai lần.

## 6. Kịch bản trình diễn

1. Mở `/SOADemo`, chọn Course Service và quan sát JSON/status thật.
2. Mở `/soa-client/index.html`; tìm `IELTS` để chứng minh client độc lập gọi cùng Course API.
3. Trong Class Service Lab chọn khóa học, xem lớp và nhấn lớp để gọi availability.
4. Đăng nhập `hocvien01@ocrs.local` / `Demo@123` (Development), chọn một lớp chưa đăng ký và gọi Registration API.
5. Gọi lại cùng lớp để nhận `409 Conflict` cùng `duplicate_registration`; hoặc chọn lớp đầy để nhận `class_unavailable`.
6. Dùng bảng contract phía trên để giải thích method, URL, DTO, auth và status code.

## 7. Chạy và kiểm tra

```powershell
dotnet restore
dotnet build
dotnet run
```

Các URL nhanh: `/Courses`, `/SOADemo`, `/soa-client/index.html`, `/api/soa/courses`.

Không dùng EF Core InMemory để kết luận về concurrency. Nếu cần test 500 request/30 chỗ, hãy dùng database SQL Server kiểm thử có thể khôi phục, các tài khoản Student riêng biệt và snapshot trước/sau; không chạy trên dữ liệu thật.

## 8. Giới hạn

Authentication hiện dùng Identity cookie của website, phù hợp demo cùng origin. Một consumer ngoài trình duyệt/ngoài origin cần thiết kế OAuth2/OIDC hoặc bearer token chuẩn trước khi mở API ghi; project cố ý không thêm endpoint nhận mật khẩu/token tạm. Availability là ảnh chụp tham khảo, quyết định giữ chỗ vẫn nằm trong transaction đăng ký.
