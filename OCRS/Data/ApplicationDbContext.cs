using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OCRS.Models;
using OCRS.Models.Enums;

namespace OCRS.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Student> Students => Set<Student>(); public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Category> Categories => Set<Category>(); public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseClass> CourseClasses => Set<CourseClass>(); public DbSet<ClassSchedule> ClassSchedules => Set<ClassSchedule>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>(); public DbSet<Notification> Notifications => Set<Notification>(); public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<CourseClass>().HasKey(x => x.ClassId); b.Entity<ClassSchedule>().HasKey(x => x.ScheduleId);
        b.Entity<ApplicationUser>().HasOne(x => x.Student).WithOne(x => x.User).HasForeignKey<Student>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ApplicationUser>().HasOne(x => x.Instructor).WithOne(x => x.User).HasForeignKey<Instructor>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Student>().HasIndex(x => x.StudentCode).IsUnique(); b.Entity<Instructor>().HasIndex(x => x.InstructorCode).IsUnique();
        b.Entity<Category>().HasIndex(x => x.CategoryName).IsUnique(); b.Entity<Course>().HasIndex(x => x.CourseCode).IsUnique(); b.Entity<Course>().HasIndex(x => x.CourseName);
        b.Entity<CourseClass>().HasIndex(x => x.ClassCode).IsUnique(); b.Entity<CourseClass>().Property(x => x.RowVersion).IsRowVersion();
        b.Entity<CourseClass>().ToTable(t => { t.HasCheckConstraint("CK_Class_Capacity", "[MaxCapacity] > 0 AND [CurrentEnrollment] >= 0 AND [CurrentEnrollment] <= [MaxCapacity]"); t.HasCheckConstraint("CK_Class_Dates", "[StartDate] <= [EndDate] AND [RegistrationOpenAt] < [RegistrationCloseAt]"); });
        b.Entity<ClassSchedule>().ToTable(t => t.HasCheckConstraint("CK_Schedule_Time", "[StartTime] < [EndTime]"));
        b.Entity<Enrollment>().HasIndex(x => new { x.StudentId, x.ClassId }).IsUnique().HasFilter($"[Status] IN ({(int)EnrollmentStatus.Pending},{(int)EnrollmentStatus.Approved})");
        b.Entity<Enrollment>().HasOne(x => x.Student).WithMany(x => x.Enrollments).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Enrollment>().HasOne(x => x.Class).WithMany(x => x.Enrollments).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CourseClass>().HasOne(x => x.Instructor).WithMany(x => x.Classes).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Course>().HasOne(x => x.Category).WithMany(x => x.Courses).OnDelete(DeleteBehavior.Restrict);
        b.Entity<CourseClass>().HasOne(x => x.Course).WithMany(x => x.Classes).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Notification>().HasOne(x => x.User).WithMany(x => x.Notifications).OnDelete(DeleteBehavior.Cascade);
    }
}
