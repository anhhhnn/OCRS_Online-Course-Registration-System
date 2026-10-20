using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OCRS.Data;
using OCRS.Models;
using OCRS.Models.Enums;
using OCRS.Services;

namespace OCRS.Tests;

public class RegistrationServiceTests
{
    private static DbContextOptions<ApplicationDbContext> Options(string name) => new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer($"Server=localhost;Database={name};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Max Pool Size=600").Options;

    [Fact]
    public async Task Register_Duplicate_Full_And_Cancel_Are_Consistent()
    {
        var name = "OCRS_Test_" + Guid.NewGuid().ToString("N"); var options = Options(name);
        await using var db = new ApplicationDbContext(options); await db.Database.EnsureCreatedAsync();
        try
        {
            var (classId, users) = await SeedAsync(db, 2, 1);
            var service = new RegistrationService(db);
            var first = await service.RegisterAsync(classId, users[0]);
            var duplicate = await service.RegisterAsync(classId, users[0]);
            var full = await service.RegisterAsync(classId, users[1]);
            Assert.True(first.Succeeded); Assert.False(duplicate.Succeeded); Assert.False(full.Succeeded);
            var cancel = await service.CancelAsync(first.EnrollmentId!.Value, users[0]); Assert.True(cancel.Succeeded);
            var cancelTwice = await service.CancelAsync(first.EnrollmentId.Value, users[0]); Assert.False(cancelTwice.Succeeded);
            Assert.Equal(0, await db.CourseClasses.Where(x => x.ClassId == classId).Select(x => x.CurrentEnrollment).SingleAsync());
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    [Fact]
    public async Task FiveHundredStudents_CompetingForThirtySeats_NeverOverbooks()
    {
        var name = "OCRS_Concurrency_" + Guid.NewGuid().ToString("N"); var options = Options(name);
        await using (var setup = new ApplicationDbContext(options)) { await setup.Database.EnsureCreatedAsync(); await SeedAsync(setup, 500, 30); }
        var watch = Stopwatch.StartNew();
        try
        {
            List<string> ids; int classId;
            await using (var read = new ApplicationDbContext(options)) { ids = await read.Students.OrderBy(x => x.StudentId).Select(x => x.UserId).ToListAsync(); classId = await read.CourseClasses.Select(x => x.ClassId).SingleAsync(); }
            var results = await Task.WhenAll(ids.Select(async id => { await using var context = new ApplicationDbContext(options); return await new RegistrationService(context).RegisterAsync(classId, id); }));
            watch.Stop();
            await using var verify = new ApplicationDbContext(options);
            var success = results.Count(x => x.Succeeded); var seats = await verify.CourseClasses.Where(x => x.ClassId == classId).Select(x => x.CurrentEnrollment).SingleAsync(); var active = await verify.Enrollments.CountAsync(x => x.ClassId == classId && (x.Status == EnrollmentStatus.Approved || x.Status == EnrollmentStatus.Pending));
            Assert.Equal(30, success); Assert.Equal(30, seats); Assert.Equal(30, active); Assert.Equal(active, await verify.Enrollments.Where(x => x.ClassId == classId).Select(x => new { x.StudentId, x.ClassId }).Distinct().CountAsync());
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "concurrency-result.txt"), $"500 requests; 30 seats; success={success}; elapsedMs={watch.ElapsedMilliseconds}; verifiedAtUtc={DateTime.UtcNow:O}");
        }
        finally { await using var cleanup = new ApplicationDbContext(options); await cleanup.Database.EnsureDeletedAsync(); }
    }

    private static async Task<(int ClassId,List<string> UserIds)> SeedAsync(ApplicationDbContext db, int students, int capacity)
    {
        var category = new Category { CategoryName = "Test " + Guid.NewGuid().ToString("N") }; var course = new Course { CourseCode = "T" + Guid.NewGuid().ToString("N")[..8], CourseName = "Concurrency", Category = category, Duration = 10, TuitionFee = 0, Status = CourseStatus.Published };
        var cls = new CourseClass { ClassCode = "C" + Guid.NewGuid().ToString("N")[..8], ClassName = "Test class", Course = course, StartDate = DateTime.UtcNow.AddDays(30), EndDate = DateTime.UtcNow.AddDays(60), RegistrationOpenAt = DateTime.UtcNow.AddDays(-1), RegistrationCloseAt = DateTime.UtcNow.AddDays(10), MaxCapacity = capacity, Status = ClassStatus.OpenForRegistration };
        db.CourseClasses.Add(cls); var ids = new List<string>();
        for (var i = 0; i < students; i++) { var id = Guid.NewGuid().ToString(); ids.Add(id); db.Users.Add(new ApplicationUser { Id = id, UserName = $"test{i}@local", NormalizedUserName = $"TEST{i}@LOCAL", Email = $"test{i}@local", NormalizedEmail = $"TEST{i}@LOCAL", FullName = $"Student {i}", SecurityStamp = Guid.NewGuid().ToString() }); db.Students.Add(new Student { UserId = id, StudentCode = $"T{i:0000}" }); }
        await db.SaveChangesAsync(); return (cls.ClassId, ids);
    }
}
