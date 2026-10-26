using Microsoft.EntityFrameworkCore;
using OCRS.Data;
using OCRS.DTOs.SOA;
using OCRS.Models;
using OCRS.Models.Enums;

namespace OCRS.Services;

public interface IClassService
{
    Task<IReadOnlyList<ClassDto>> GetForCourseAsync(int courseId, CancellationToken ct = default);
    Task<ClassDto?> GetAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ClassScheduleDto>?> GetScheduleAsync(int id, CancellationToken ct = default);
    Task<ClassAvailabilityDto?> GetAvailabilityAsync(int id, CancellationToken ct = default);
}

public sealed class ClassService(ApplicationDbContext db) : IClassService
{
    private static int Remaining(CourseClass value) => Math.Max(0, value.MaxCapacity - value.CurrentEnrollment);

    private static string RegistrationState(CourseClass value, DateTime now)
    {
        if (value.Status == ClassStatus.OpenForRegistration && value.RegistrationOpenAt <= now && value.RegistrationCloseAt >= now)
            return Remaining(value) == 0 ? "Full" : "OpenForRegistration";
        if (value.Status == ClassStatus.Draft || now < value.RegistrationOpenAt) return "NotOpen";
        return "Closed";
    }

    private static ClassDto Map(CourseClass value, DateTime now) => new(
        value.ClassId, value.ClassCode, value.ClassName, value.CourseId, value.Course.CourseName,
        value.Instructor?.User.FullName, value.StartDate, value.EndDate, value.RegistrationOpenAt,
        value.RegistrationCloseAt, value.MaxCapacity, value.CurrentEnrollment, Remaining(value),
        value.LearningMode, value.Location, value.Status, RegistrationState(value, now), value.RequiresApproval);

    private IQueryable<CourseClass> Query() => db.CourseClasses.AsNoTracking()
        .Include(x => x.Course).Include(x => x.Instructor).ThenInclude(x => x!.User);

    public async Task<IReadOnlyList<ClassDto>> GetForCourseAsync(int courseId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var items = await Query().Where(x => x.CourseId == courseId).OrderBy(x => x.StartDate).ToListAsync(ct);
        return items.Select(x => Map(x, now)).ToList();
    }

    public async Task<ClassDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var item = await Query().SingleOrDefaultAsync(x => x.ClassId == id, ct);
        return item is null ? null : Map(item, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<ClassScheduleDto>?> GetScheduleAsync(int id, CancellationToken ct = default)
    {
        if (!await db.CourseClasses.AsNoTracking().AnyAsync(x => x.ClassId == id, ct)) return null;
        return await db.ClassSchedules.AsNoTracking().Where(x => x.ClassId == id)
            .OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime)
            .Select(x => new ClassScheduleDto(x.ScheduleId, x.DayOfWeek, x.StartTime, x.EndTime, x.RoomOrLink, x.Note))
            .ToListAsync(ct);
    }

    public async Task<ClassAvailabilityDto?> GetAvailabilityAsync(int id, CancellationToken ct = default)
    {
        var item = await db.CourseClasses.AsNoTracking().SingleOrDefaultAsync(x => x.ClassId == id, ct);
        if (item is null) return null;
        return new(item.ClassId, item.MaxCapacity, item.CurrentEnrollment, Remaining(item), RegistrationState(item, DateTime.UtcNow));
    }
}
