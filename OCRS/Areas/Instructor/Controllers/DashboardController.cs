using System.Security.Claims; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using OCRS.Data;
namespace OCRS.Areas.Instructor.Controllers;
[Area("Instructor"),Authorize(Roles="Instructor")] public class DashboardController(ApplicationDbContext db):Controller { public async Task<IActionResult> Index(){var uid=User.FindFirstValue(ClaimTypes.NameIdentifier);return View(await db.CourseClasses.Include(x=>x.Course).Include(x=>x.Enrollments).Where(x=>x.Instructor!.UserId==uid).OrderBy(x=>x.StartDate).ToListAsync());} }
