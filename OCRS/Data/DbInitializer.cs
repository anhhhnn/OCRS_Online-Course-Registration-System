using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OCRS.Models;
using OCRS.Models.Enums;

namespace OCRS.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IWebHostEnvironment env)
    {
        using var scope = services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Instructor", "Student" }) if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));
        if (!env.IsDevelopment()) return;
        await EnsureUser(users, db, "admin@ocrs.local", "Quản trị OCRS", "Admin", "Admin@123", 0);
        for (var i = 1; i <= 3; i++) await EnsureUser(users, db, $"giangvien{i}@ocrs.local", $"Giảng viên {i}", "Instructor", "Demo@123", i);
        for (var i = 1; i <= 15; i++) await EnsureUser(users, db, $"hocvien{i:00}@ocrs.local", $"Học viên {i:00}", "Student", "Demo@123", i);
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(new[] { "Tiếng Anh", "Tiếng Nhật", "Tiếng Trung", "Tiếng Hàn", "Tin học", "Kỹ năng mềm" }.Select((n,i) => new Category { CategoryName=n, Description=$"Các chương trình {n} chất lượng cao" })); await db.SaveChangesAsync();
        }
        if (!await db.Courses.AnyAsync())
        {
            var cats = await db.Categories.OrderBy(x=>x.CategoryId).ToListAsync();
            for (var i=1;i<=12;i++) db.Courses.Add(new Course { CourseCode=$"CRS{i:000}", CourseName=i==1?"IELTS Foundation":$"Khóa học {cats[(i-1)%cats.Count].CategoryName} {i}", CategoryId=cats[(i-1)%cats.Count].CategoryId, Description="Lộ trình thực tiễn, học cùng giảng viên giàu kinh nghiệm.", Objectives="Phát triển kiến thức nền tảng và khả năng ứng dụng.", Duration=36+i*2, TuitionFee=2500000+i*150000, LearningMode=(LearningMode)(i%3), Status=CourseStatus.Published });
            await db.SaveChangesAsync();
        }
        if (!await db.CourseClasses.AnyAsync())
        {
            var cs=await db.Courses.OrderBy(x=>x.CourseId).ToListAsync(); var ins=await db.Instructors.OrderBy(x=>x.InstructorId).ToListAsync(); var now=DateTime.UtcNow;
            for(var i=1;i<=20;i++){ var demo=i==1; var c=new CourseClass { ClassCode=demo?"IELTS-DEMO":$"CLS{i:000}", ClassName=demo?"IELTS Foundation Demo":$"Lớp {i:00} - {cs[(i-1)%cs.Count].CourseName}", CourseId=cs[(i-1)%cs.Count].CourseId, InstructorId=ins[(i-1)%ins.Count].InstructorId, StartDate=now.Date.AddDays(30+i), EndDate=now.Date.AddDays(120+i), RegistrationOpenAt=now.AddDays(-10), RegistrationCloseAt=now.AddDays(20+i), MaxCapacity=demo?30:20+i, CurrentEnrollment=0, LearningMode=(LearningMode)(i%3), Location=i%3==0?"Cơ sở Ocean Edu":"Phòng học trực tuyến", MeetingLink=i%3==0?null:"https://meet.example.local/ocrs", Status=ClassStatus.OpenForRegistration, RequiresApproval=i%4==0 }; c.Schedules.Add(new ClassSchedule { DayOfWeek=(DayOfWeek)((i%6)+1), StartTime=new TimeOnly(18,0), EndTime=new TimeOnly(20,0), RoomOrLink=c.Location }); db.CourseClasses.Add(c); }
            await db.SaveChangesAsync();
        }
        if (!await db.Enrollments.AnyAsync())
        {
            var students = await db.Students.Include(x => x.User).OrderBy(x => x.StudentId).Take(6).ToListAsync();
            var classes = await db.CourseClasses.OrderBy(x => x.ClassId).Take(6).ToListAsync();
            for (var i = 0; i < Math.Min(students.Count, classes.Count); i++)
            {
                db.Enrollments.Add(new Enrollment { StudentId = students[i].StudentId, ClassId = classes[i].ClassId, Status = EnrollmentStatus.Approved, ApprovedAt = DateTime.UtcNow });
                classes[i].CurrentEnrollment++;
                db.Notifications.Add(new Notification { UserId = students[i].UserId, Title = "Chào mừng đến OCRS", Content = $"Bạn đã được ghi danh vào {classes[i].ClassName}.", Type = NotificationType.Success });
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureUser(UserManager<ApplicationUser> um, ApplicationDbContext db, string email, string name, string role, string password, int number)
    {
        var u=await um.FindByEmailAsync(email); if(u is null){ u=new ApplicationUser { UserName=email,Email=email,FullName=name,EmailConfirmed=true,PhoneNumber=$"090000{number:0000}" }; var result=await um.CreateAsync(u,password); if(!result.Succeeded) throw new InvalidOperationException(string.Join("; ",result.Errors.Select(x=>x.Description))); await um.AddToRoleAsync(u,role); if(role=="Student") db.Students.Add(new Student{UserId=u.Id,StudentCode=$"HV{number:0000}"}); if(role=="Instructor") db.Instructors.Add(new Instructor{UserId=u.Id,InstructorCode=$"GV{number:000}",Specialization="Ngoại ngữ và kỹ năng"}); await db.SaveChangesAsync(); }
    }
}
