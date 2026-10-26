using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCRS.Data;
using OCRS.Models.Enums;
using OCRS.ViewModels.Catalog;

namespace OCRS.Controllers;

public sealed class ClassesController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var item = await db.CourseClasses.AsNoTracking().Include(x => x.Course).ThenInclude(x => x.Category)
            .Include(x => x.Instructor).ThenInclude(x => x!.User).Include(x => x.Schedules)
            .SingleOrDefaultAsync(x => x.ClassId == id, ct);
        if (item is null) return NotFound();

        EnrollmentSnapshot? enrollment = null;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            enrollment = await db.Enrollments.AsNoTracking()
                .Where(x => x.ClassId == id && x.Student.UserId == userId)
                .OrderByDescending(x => x.RegisteredAt)
                .Select(x => new EnrollmentSnapshot(x.EnrollmentId, x.Status)).FirstOrDefaultAsync(ct);
        }
        var now = DateTime.UtcNow;
        var others = await db.CourseClasses.AsNoTracking().Include(x => x.Instructor).ThenInclude(x => x!.User)
            .Where(x => x.CourseId == item.CourseId && x.ClassId != id)
            .OrderByDescending(x => x.Status == ClassStatus.OpenForRegistration && x.RegistrationOpenAt <= now && x.RegistrationCloseAt >= now && x.CurrentEnrollment < x.MaxCapacity)
            .ThenBy(x => x.StartDate).Take(3).ToListAsync(ct);
        return View(new ClassDetailsViewModel { Class = item, Enrollment = enrollment, OtherClasses = others });
    }
}
