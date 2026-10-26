using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using OCRS.Data;
using OCRS.Models.Enums;
using OCRS.Services;
using OCRS.ViewModels.Catalog;

namespace OCRS.Controllers;

public sealed class CoursesController(ICourseService courses, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int? category, LearningMode? mode, int page = 1, CancellationToken ct = default)
    {
        const int size = 9;
        page = Math.Max(1, page);
        var result = await courses.GetPublishedPageAsync(q, category, mode, page, size, ct);
        ViewBag.Categories = await db.Categories.AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        ViewBag.Page = page;
        ViewBag.Total = result.TotalPages;
        return View(result.Items);
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var course = await courses.GetPublishedDetailsAsync(id, ct);
        if (course is null) return NotFound();
        var enrollmentByClass = new Dictionary<int, EnrollmentSnapshot>();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            enrollmentByClass = await db.Enrollments.AsNoTracking()
                .Where(x => x.Student.UserId == userId && x.Class.CourseId == id)
                .GroupBy(x => x.ClassId)
                .Select(g => g.OrderByDescending(x => x.RegisteredAt).First())
                .ToDictionaryAsync(x => x.ClassId, x => new EnrollmentSnapshot(x.EnrollmentId, x.Status), ct);
        }
        var now = DateTime.UtcNow;
        var cards = course.Classes
            .OrderByDescending(x => x.Status == ClassStatus.OpenForRegistration && x.RegistrationOpenAt <= now && x.RegistrationCloseAt >= now && x.CurrentEnrollment < x.MaxCapacity)
            .ThenBy(x => x.StartDate)
            .Select(x => new ClassCardViewModel { Class = x, Enrollment = enrollmentByClass.GetValueOrDefault(x.ClassId) }).ToList();
        return View(new CourseDetailsViewModel { Course = course, Classes = cards });
    }
}
