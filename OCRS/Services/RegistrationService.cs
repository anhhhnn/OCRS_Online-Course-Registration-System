using System.Data;
using Microsoft.EntityFrameworkCore;
using OCRS.Data;
using OCRS.DTOs.SOA;
using OCRS.Models;
using OCRS.Models.Enums;

namespace OCRS.Services;

public record RegistrationResult(bool Succeeded, string Message, int? EnrollmentId = null, string Status = "Rejected", string? ErrorCode = null);

public interface IRegistrationService
{
    Task<RegistrationResult> RegisterAsync(int classId, string userId, CancellationToken ct = default);
    Task<RegistrationResult> CancelAsync(int enrollmentId, string userId, bool isAdmin = false, CancellationToken ct = default);
    Task<IReadOnlyList<EnrollmentDto>> GetMineAsync(string userId, CancellationToken ct = default);
    Task<MyRegistrationStatusDto> GetMyStatusAsync(int classId, string userId, CancellationToken ct = default);
}

public sealed class RegistrationService(ApplicationDbContext db) : IRegistrationService
{
    public async Task<RegistrationResult> RegisterAsync(int classId, string userId, CancellationToken ct = default)
    {
        var student = await db.Students.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (student is null) return new(false, "Không tìm thấy hồ sơ học viên.", Status: "Rejected", ErrorCode: "student_not_found");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var existing = await db.Enrollments.AsNoTracking().FirstOrDefaultAsync(x => x.StudentId == student.StudentId && x.ClassId == classId && (x.Status == EnrollmentStatus.Pending || x.Status == EnrollmentStatus.Approved), ct);
            if (existing is not null) return new(false, "Bạn đã đăng ký lớp này.", existing.EnrollmentId, existing.Status == EnrollmentStatus.Pending ? "Pending" : "Confirmed", "duplicate_registration");
            if (!await db.CourseClasses.AsNoTracking().AnyAsync(x => x.ClassId == classId, ct)) return new(false, "Không tìm thấy lớp học.", Status: "Rejected", ErrorCode: "class_not_found");
            var now = DateTime.UtcNow;
            var changed = await db.CourseClasses.Where(x => x.ClassId == classId && x.Status == ClassStatus.OpenForRegistration && x.RegistrationOpenAt <= now && x.RegistrationCloseAt >= now && x.CurrentEnrollment < x.MaxCapacity)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.CurrentEnrollment, x => x.CurrentEnrollment + 1), ct);
            if (changed == 0) return new(false, "Lớp chưa mở đăng ký, đã đóng hoặc đã đủ chỗ.", Status: "Rejected", ErrorCode: "class_unavailable");
            var courseClass = await db.CourseClasses.AsNoTracking().SingleAsync(x => x.ClassId == classId, ct);
            var status = courseClass.RequiresApproval ? EnrollmentStatus.Pending : EnrollmentStatus.Approved;
            var enrollment = new Enrollment { StudentId = student.StudentId, ClassId = classId, Status = status, ApprovedAt = courseClass.RequiresApproval ? null : now };
            db.Enrollments.Add(enrollment);
            await db.SaveChangesAsync(ct);
            db.Notifications.Add(new Notification { UserId = userId, Title = "Đăng ký lớp học", Content = courseClass.RequiresApproval ? "Yêu cầu đang chờ quản trị viên duyệt." : "Bạn đã đăng ký lớp thành công.", Type = NotificationType.Success, RelatedEntityId = enrollment.EnrollmentId });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return new(true, courseClass.RequiresApproval ? "Yêu cầu đăng ký đang chờ duyệt." : "Đăng ký lớp học thành công.", enrollment.EnrollmentId, courseClass.RequiresApproval ? "Pending" : "Confirmed");
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(ct);
            return new(false, "Đăng ký trùng hoặc dữ liệu vừa được thay đổi. Vui lòng thử lại.", Status: "Rejected", ErrorCode: "registration_conflict");
        }
    }

    public async Task<RegistrationResult> CancelAsync(int enrollmentId, string userId, bool isAdmin = false, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var enrollment = await db.Enrollments.AsNoTracking().Include(x => x.Student).SingleOrDefaultAsync(x => x.EnrollmentId == enrollmentId, ct);
        if (enrollment is null) return new(false, "Không tìm thấy đăng ký.", Status: "Rejected", ErrorCode: "enrollment_not_found");
        if (!isAdmin && enrollment.Student.UserId != userId) return new(false, "Bạn không có quyền hủy đăng ký này.", Status: "Rejected", ErrorCode: "forbidden");
        if (enrollment.Status is not (EnrollmentStatus.Pending or EnrollmentStatus.Approved)) return new(false, "Đăng ký không còn hiệu lực.", enrollment.EnrollmentId, enrollment.Status.ToString(), "already_inactive");
        var now = DateTime.UtcNow;
        var changed = await db.Enrollments
            .Where(x => x.EnrollmentId == enrollmentId && (x.Status == EnrollmentStatus.Pending || x.Status == EnrollmentStatus.Approved))
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, EnrollmentStatus.Cancelled).SetProperty(x => x.CancelledAt, now), ct);
        if (changed == 0) { await tx.RollbackAsync(ct); return new(false, "Đăng ký đã được xử lý bởi một yêu cầu khác.", enrollment.EnrollmentId, "Cancelled", "already_inactive"); }
        await db.CourseClasses.Where(x => x.ClassId == enrollment.ClassId && x.CurrentEnrollment > 0).ExecuteUpdateAsync(update => update.SetProperty(x => x.CurrentEnrollment, x => x.CurrentEnrollment - 1), ct);
        db.Notifications.Add(new Notification { UserId = enrollment.Student.UserId, Title = "Hủy đăng ký", Content = "Đăng ký lớp học đã được hủy.", Type = NotificationType.Information, RelatedEntityId = enrollment.EnrollmentId });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new(true, "Hủy đăng ký thành công.", enrollment.EnrollmentId, "Cancelled");
    }

    public async Task<IReadOnlyList<EnrollmentDto>> GetMineAsync(string userId, CancellationToken ct = default) =>
        await db.Enrollments.AsNoTracking().Where(x => x.Student.UserId == userId).OrderByDescending(x => x.RegisteredAt)
            .Select(x => new EnrollmentDto(x.EnrollmentId, x.ClassId, x.Class.ClassCode, x.Class.ClassName, x.Class.Course.CourseName, x.Status == EnrollmentStatus.Approved ? "Confirmed" : x.Status.ToString(), x.RegisteredAt, x.CancelledAt)).ToListAsync(ct);

    public async Task<MyRegistrationStatusDto> GetMyStatusAsync(int classId, string userId, CancellationToken ct = default)
    {
        var item = await db.Enrollments.AsNoTracking().Where(x => x.ClassId == classId && x.Student.UserId == userId).OrderByDescending(x => x.RegisteredAt).Select(x => new { x.EnrollmentId, x.Status }).FirstOrDefaultAsync(ct);
        if (item is null) return new(classId, false, null, null);
        var status = item.Status == EnrollmentStatus.Approved ? "Confirmed" : item.Status.ToString();
        return new(classId, item.Status is EnrollmentStatus.Pending or EnrollmentStatus.Approved, item.EnrollmentId, status);
    }
}
