using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore; using OCRS.Data; using OCRS.Models.Enums;
namespace OCRS.Controllers;
public class CoursesController(ApplicationDbContext db):Controller
{
 public async Task<IActionResult> Index(string? q,int? category,LearningMode? mode,int page=1){const int size=9;var x=db.Courses.AsNoTracking().Include(c=>c.Category).Where(c=>c.Status==CourseStatus.Published);if(!string.IsNullOrWhiteSpace(q))x=x.Where(c=>c.CourseName.Contains(q)||c.CourseCode.Contains(q));if(category.HasValue)x=x.Where(c=>c.CategoryId==category);if(mode.HasValue)x=x.Where(c=>c.LearningMode==mode);ViewBag.Categories=await db.Categories.Where(c=>c.IsActive).ToListAsync();ViewBag.Page=page;ViewBag.Total=(int)Math.Ceiling(await x.CountAsync()/(double)size);return View(await x.OrderBy(c=>c.CourseName).Skip((page-1)*size).Take(size).ToListAsync());}
 public async Task<IActionResult> Details(int id){var c=await db.Courses.AsNoTracking().Include(x=>x.Category).Include(x=>x.Classes.Where(k=>k.Status==ClassStatus.OpenForRegistration)).ThenInclude(x=>x.Instructor).ThenInclude(x=>x!.User).Include(x=>x.Classes).ThenInclude(x=>x.Schedules).SingleOrDefaultAsync(x=>x.CourseId==id&&x.Status==CourseStatus.Published);return c==null?NotFound():View(c);}
}
