using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCRS.Data;
using OCRS.Models;
using OCRS.Models.Enums;

namespace OCRS.Controllers;

public class HomeController(ILogger<HomeController> logger, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await db.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Include(x => x.Courses.Where(c => c.Status == CourseStatus.Published))
            .OrderBy(x => x.CategoryName)
            .Take(6)
            .ToListAsync();

        ViewBag.Instructors = await db.Instructors
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Classes)
            .OrderBy(x => x.User.FullName)
            .Take(3)
            .ToListAsync();

        return View(await db.Courses
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.Status == CourseStatus.Published)
            .OrderByDescending(x => x.CreatedAt)
            .Take(6)
            .ToListAsync());
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        logger.LogError("Yêu cầu lỗi: {RequestId}", HttpContext.TraceIdentifier);
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult AccessDenied() => View();
    public IActionResult HttpStatus(int code) { ViewBag.Code = code; return View("StatusCode"); }
}
