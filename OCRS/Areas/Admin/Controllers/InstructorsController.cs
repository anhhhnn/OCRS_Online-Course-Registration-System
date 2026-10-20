using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OCRS.Data;
using OCRS.Models;

namespace OCRS.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public class InstructorsController(ApplicationDbContext db, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Instructors.Include(x => x.User).Include(x => x.Classes).OrderBy(x => x.User.FullName).ToListAsync());
    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string fullName, string email, string specialization)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email)) { ModelState.AddModelError("", "Vui lòng nhập đủ họ tên và email."); return View(); }
        var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, EmailConfirmed = true };
        var result = await users.CreateAsync(user, "Teacher@123");
        if (!result.Succeeded) { foreach (var error in result.Errors) ModelState.AddModelError("", error.Description); return View(); }
        await users.AddToRoleAsync(user, "Instructor");
        db.Instructors.Add(new OCRS.Models.Instructor { UserId = user.Id, InstructorCode = $"GV{DateTime.UtcNow:yyMMddHHmm}", Specialization = specialization });
        await db.SaveChangesAsync(); TempData["Success"] = "Đã tạo giảng viên. Mật khẩu tạm: Teacher@123"; return RedirectToAction(nameof(Index));
    }
}
