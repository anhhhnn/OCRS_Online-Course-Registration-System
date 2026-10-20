using System.Security.Claims; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using OCRS.Data;
namespace OCRS.Controllers;
[Authorize(Roles="Student")] public class MyCoursesController(ApplicationDbContext db):Controller 
{ public async Task<IActionResult>
 Index(){var uid=User.FindFirstValue(ClaimTypes.NameIdentifier);
return View(await db.Enrollments.AsNoTracking().Include(x=>x.Class).ThenInclude(x=>x.Course).Include(x=>x.Class).ThenInclude(x=>x.Schedules).Where(x=>x.Student.UserId==uid).OrderByDescending(x=>x.RegisteredAt).ToListAsync());} }
