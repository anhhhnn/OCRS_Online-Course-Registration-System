using System.Data; using Microsoft.EntityFrameworkCore; using OCRS.Data; using OCRS.Models; using OCRS.Models.Enums;
namespace OCRS.Services;
public record RegistrationResult(bool Succeeded,string Message,int? EnrollmentId=null);
public interface IRegistrationService { Task<RegistrationResult> RegisterAsync(int classId,string userId,CancellationToken ct=default); Task<RegistrationResult> CancelAsync(int enrollmentId,string userId,bool isAdmin=false,CancellationToken ct=default); }
public class RegistrationService(ApplicationDbContext db) : IRegistrationService
{
    public async Task<RegistrationResult> RegisterAsync(int classId,string userId,CancellationToken ct=default)
    {
        var student=await db.Students.SingleOrDefaultAsync(x=>x.UserId==userId,ct); if(student is null) return new(false,"Không tìm thấy hồ sơ học viên.");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted,ct);
        try
        {
            if(await db.Enrollments.AnyAsync(x=>x.StudentId==student.StudentId&&x.ClassId==classId&&(x.Status==EnrollmentStatus.Pending||x.Status==EnrollmentStatus.Approved),ct)) return new(false,"Bạn đã đăng ký lớp này.");
            var now=DateTime.UtcNow;
            var changed=await db.CourseClasses.Where(x=>x.ClassId==classId&&x.Status==ClassStatus.OpenForRegistration&&x.RegistrationOpenAt<=now&&x.RegistrationCloseAt>=now&&x.CurrentEnrollment<x.MaxCapacity).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.CurrentEnrollment,x=>x.CurrentEnrollment+1),ct);
            if(changed==0) return new(false,"Lớp chưa mở đăng ký, đã đóng hoặc đã đủ chỗ.");
            var cls=await db.CourseClasses.AsNoTracking().SingleAsync(x=>x.ClassId==classId,ct); var e=new Enrollment{StudentId=student.StudentId,ClassId=classId,Status=cls.RequiresApproval?EnrollmentStatus.Pending:EnrollmentStatus.Approved,ApprovedAt=cls.RequiresApproval?null:now}; db.Enrollments.Add(e); await db.SaveChangesAsync(ct);
            db.Notifications.Add(new Notification{UserId=userId,Title="Đăng ký lớp học",Content=cls.RequiresApproval?"Yêu cầu đang chờ quản trị viên duyệt.":"Bạn đã đăng ký lớp thành công.",Type=NotificationType.Success,RelatedEntityId=e.EnrollmentId}); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(true,cls.RequiresApproval?"Đã gửi yêu cầu đăng ký.":"Đăng ký thành công.",e.EnrollmentId);
        }
        catch(DbUpdateException){ await tx.RollbackAsync(ct); return new(false,"Đăng ký trùng hoặc dữ liệu vừa được thay đổi. Vui lòng thử lại."); }
    }
    public async Task<RegistrationResult> CancelAsync(int enrollmentId,string userId,bool isAdmin=false,CancellationToken ct=default)
    {
        await using var tx=await db.Database.BeginTransactionAsync(ct); var e=await db.Enrollments.Include(x=>x.Student).SingleOrDefaultAsync(x=>x.EnrollmentId==enrollmentId,ct); if(e is null) return new(false,"Không tìm thấy đăng ký."); if(!isAdmin&&e.Student.UserId!=userId) return new(false,"Bạn không có quyền hủy đăng ký này."); if(e.Status is not (EnrollmentStatus.Pending or EnrollmentStatus.Approved)) return new(false,"Đăng ký không còn hiệu lực."); e.Status=EnrollmentStatus.Cancelled; e.CancelledAt=DateTime.UtcNow; await db.CourseClasses.Where(x=>x.ClassId==e.ClassId&&x.CurrentEnrollment>0).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.CurrentEnrollment,x=>x.CurrentEnrollment-1),ct); await db.SaveChangesAsync(ct); db.Notifications.Add(new Notification{UserId=e.Student.UserId,Title="Hủy đăng ký",Content="Đăng ký lớp học đã được hủy.",Type=NotificationType.Information,RelatedEntityId=e.EnrollmentId}); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(true,"Hủy đăng ký thành công.");
    }
}
