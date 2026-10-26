using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using OCRS.Data;
using OCRS.Models;
using OCRS.Models.Enums;
using OCRS.Services;
using OCRS.ViewModels.Admin;
using AdminClassesController = OCRS.Areas.Admin.Controllers.ClassesController;

namespace OCRS.Tests;

public sealed class SqlServerRegistrationTests : IAsyncLifetime
{
    private readonly string _database = $"OCRS_Test_{Guid.NewGuid():N}";
    private string Connection => $"Server=localhost;Database={_database};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
    private DbContextOptions<ApplicationDbContext> Options => new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(Connection).Options;
    private int _classId;

    public async Task InitializeAsync()
    {
        await using var db=new ApplicationDbContext(Options);await db.Database.MigrateAsync();
        var category=new Category{CategoryName="Integration Test"};db.Categories.Add(category);await db.SaveChangesAsync();
        var course=new Course{CourseCode="IT-COURSE",CourseName="Integration Course",CategoryId=category.CategoryId,Duration=10,Status=CourseStatus.Published};db.Courses.Add(course);await db.SaveChangesAsync();
        var cls=new CourseClass{ClassCode="IT-CLASS",ClassName="Integration Class",CourseId=course.CourseId,StartDate=DateTime.UtcNow.AddDays(10),EndDate=DateTime.UtcNow.AddDays(20),RegistrationOpenAt=DateTime.UtcNow.AddDays(-1),RegistrationCloseAt=DateTime.UtcNow.AddDays(2),MaxCapacity=30,Status=ClassStatus.OpenForRegistration};db.CourseClasses.Add(cls);
        for(var i=0;i<500;i++){var user=new ApplicationUser{Id=$"test-user-{i}",UserName=$"test{i}@local",NormalizedUserName=$"TEST{i}@LOCAL",Email=$"test{i}@local",NormalizedEmail=$"TEST{i}@LOCAL",FullName=$"Student {i}"};db.Users.Add(user);db.Students.Add(new Student{UserId=user.Id,StudentCode=$"TEST{i:0000}"});}
        await db.SaveChangesAsync();_classId=cls.ClassId;
    }

    public async Task DisposeAsync()
    {
        await using var db=new ApplicationDbContext(Options);await db.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task Duplicate_registration_and_repeated_cancel_change_seat_once()
    {
        var registrations=await Task.WhenAll(Enumerable.Range(0,8).Select(async _=>{await using var db=new ApplicationDbContext(Options);return await new RegistrationService(db).RegisterAsync(_classId,"test-user-0");}));
        var enrollmentId=registrations.Single(x=>x.Succeeded).EnrollmentId!.Value;
        await Task.WhenAll(Enumerable.Range(0,8).Select(async _=>{await using var db=new ApplicationDbContext(Options);return await new RegistrationService(db).CancelAsync(enrollmentId,"test-user-0");}));
        await using var verify=new ApplicationDbContext(Options);var cls=await verify.CourseClasses.SingleAsync(x=>x.ClassId==_classId);var rows=await verify.Enrollments.CountAsync(x=>x.ClassId==_classId&&x.Student.UserId=="test-user-0");
        Assert.Single(registrations,x=>x.Succeeded);Assert.Equal(1,rows);Assert.Equal(0,cls.CurrentEnrollment);
    }

    [Fact]
    public async Task Admin_controller_create_and_edit_persist_to_sql_server()
    {
        await using var db=new ApplicationDbContext(Options);var courseId=await db.Courses.Select(x=>x.CourseId).SingleAsync();
        var controller=new AdminClassesController(db,NullLogger<AdminClassesController>.Instance){TempData=new TempDataDictionary(new DefaultHttpContext(),new TestTempDataProvider())};
        var create=new ClassFormViewModel{ClassCode="ADMIN-TEST",ClassName="Admin created",CourseId=courseId,StartDate=DateTime.Today.AddDays(5),EndDate=DateTime.Today.AddDays(15),RegistrationOpenAt=DateTime.Now.AddDays(-1),RegistrationCloseAt=DateTime.Now.AddDays(3),MaxCapacity=12,LearningMode=LearningMode.Offline,Location="Room 1",Status=ClassStatus.OpenForRegistration};
        Assert.IsType<RedirectToActionResult>(await controller.Create(create,default));
        var saved=await db.CourseClasses.AsNoTracking().SingleAsync(x=>x.ClassCode=="ADMIN-TEST");
        var edit=new ClassFormViewModel{ClassId=saved.ClassId,ClassCode=saved.ClassCode,ClassName="Admin updated",CourseId=saved.CourseId,StartDate=saved.StartDate,EndDate=saved.EndDate,RegistrationOpenAt=saved.RegistrationOpenAt,RegistrationCloseAt=saved.RegistrationCloseAt,MaxCapacity=15,LearningMode=saved.LearningMode,Location=saved.Location,Status=saved.Status,RowVersion=Convert.ToBase64String(saved.RowVersion)};
        Assert.IsType<RedirectToActionResult>(await controller.Edit(saved.ClassId,edit,default));
        var updated=await db.CourseClasses.AsNoTracking().SingleAsync(x=>x.ClassId==saved.ClassId);Assert.Equal("Admin updated",updated.ClassName);Assert.Equal(15,updated.MaxCapacity);
    }

    [Fact]
    public async Task Approval_required_registration_is_persisted_as_pending()
    {
        await using(var setup=new ApplicationDbContext(Options)){await setup.CourseClasses.Where(x=>x.ClassId==_classId).ExecuteUpdateAsync(x=>x.SetProperty(c=>c.RequiresApproval,true));}
        await using var db=new ApplicationDbContext(Options);var result=await new RegistrationService(db).RegisterAsync(_classId,"test-user-1");
        var status=await db.Enrollments.Where(x=>x.EnrollmentId==result.EnrollmentId).Select(x=>x.Status).SingleAsync();
        Assert.True(result.Succeeded);Assert.Equal("Pending",result.Status);Assert.Equal(EnrollmentStatus.Pending,status);
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string,object> LoadTempData(HttpContext context)=>new Dictionary<string,object>();
        public void SaveTempData(HttpContext context,IDictionary<string,object> values){}
    }
}
