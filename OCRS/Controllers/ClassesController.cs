using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using OCRS.Data;
namespace OCRS.Controllers;
public class ClassesController(ApplicationDbContext db):Controller { public async Task<IActionResult> Details(int id){var c=await db.CourseClasses.AsNoTracking().Include(x=>x.Course).ThenInclude(x=>x.Category).Include(x=>x.Instructor).ThenInclude(x=>x!.User).Include(x=>x.Schedules).SingleOrDefaultAsync(x=>x.ClassId==id);return c==null?NotFound():View(c);} }
