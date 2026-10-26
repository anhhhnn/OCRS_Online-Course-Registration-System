using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OCRS.Data;
using OCRS.Models;
using OCRS.ViewModels.Admin;

namespace OCRS.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = "Admin")]
public sealed class ClassesController(ApplicationDbContext db, ILogger<ClassesController> logger) : Controller
{
    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        var query = db.CourseClasses.AsNoTracking().Include(x => x.Course).Include(x => x.Instructor).ThenInclude(x => x!.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) { var term = q.Trim(); query = query.Where(x => x.ClassName.Contains(term) || x.ClassCode.Contains(term)); }
        return View(await query.OrderByDescending(x => x.StartDate).ToListAsync(ct));
    }

    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await PopulateListsAsync(ct);
        return View("Form", new ClassFormViewModel { StartDate = DateTime.Today.AddDays(30), EndDate = DateTime.Today.AddDays(120), RegistrationOpenAt = DateTime.Now, RegistrationCloseAt = DateTime.Now.AddDays(20) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassFormViewModel model, CancellationToken ct)
    {
        await ValidateReferencesAndCodeAsync(model, null, ct);
        if (!ModelState.IsValid) return await InvalidFormAsync(model, ct);
        var entity = new CourseClass(); Apply(model, entity); db.CourseClasses.Add(entity);
        try { await db.SaveChangesAsync(ct); TempData["Success"] = "Lưu lớp học thành công."; return RedirectToAction(nameof(Index)); }
        catch (DbUpdateException ex) { logger.LogError(ex, "Không thể tạo lớp {ClassCode}", model.ClassCode); ModelState.AddModelError(string.Empty, "Không thể lưu lớp học. Dữ liệu có thể bị trùng hoặc không còn hợp lệ."); return await InvalidFormAsync(model, ct); }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var entity = await db.CourseClasses.AsNoTracking().SingleOrDefaultAsync(x => x.ClassId == id, ct);
        if (entity is null) return NotFound();
        await PopulateListsAsync(ct); return View("Form", Map(entity));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ClassFormViewModel model, CancellationToken ct)
    {
        if (id != model.ClassId) return BadRequest();
        var entity = await db.CourseClasses.SingleOrDefaultAsync(x => x.ClassId == id, ct);
        if (entity is null) return NotFound();
        if (model.MaxCapacity < entity.CurrentEnrollment) ModelState.AddModelError(nameof(model.MaxCapacity), "Sĩ số tối đa không thể thấp hơn số đăng ký hiện tại.");
        await ValidateReferencesAndCodeAsync(model, id, ct);
        if (string.IsNullOrWhiteSpace(model.RowVersion)) ModelState.AddModelError(string.Empty, "Phiên chỉnh sửa không hợp lệ. Vui lòng tải lại trang.");
        else { try { db.Entry(entity).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(model.RowVersion); } catch (FormatException) { ModelState.AddModelError(string.Empty, "Phiên chỉnh sửa không hợp lệ. Vui lòng tải lại trang."); } }
        if (!ModelState.IsValid) return await InvalidFormAsync(model, ct);
        Apply(model, entity);
        try { await db.SaveChangesAsync(ct); TempData["Success"] = "Lưu lớp học thành công."; return RedirectToAction(nameof(Index)); }
        catch (DbUpdateConcurrencyException ex) { logger.LogWarning(ex, "Xung đột khi cập nhật lớp {ClassId}", id); ModelState.AddModelError(string.Empty, "Lớp vừa được người khác cập nhật. Vui lòng tải lại trang và thử lại."); model.RowVersion = Convert.ToBase64String((await ex.Entries.Single().GetDatabaseValuesAsync(ct))?.GetValue<byte[]>(nameof(CourseClass.RowVersion)) ?? []); return await InvalidFormAsync(model, ct); }
        catch (DbUpdateException ex) { logger.LogError(ex, "Không thể cập nhật lớp {ClassId}", id); ModelState.AddModelError(string.Empty, "Không thể lưu lớp học. Dữ liệu có thể bị trùng hoặc không còn hợp lệ."); return await InvalidFormAsync(model, ct); }
    }

    private async Task ValidateReferencesAndCodeAsync(ClassFormViewModel model, int? currentId, CancellationToken ct)
    {
        model.ClassCode = model.ClassCode.Trim(); model.ClassName = model.ClassName.Trim();
        if (!await db.Courses.AsNoTracking().AnyAsync(x => x.CourseId == model.CourseId, ct)) ModelState.AddModelError(nameof(model.CourseId), "Khóa học không tồn tại.");
        if (model.InstructorId.HasValue && !await db.Instructors.AsNoTracking().AnyAsync(x => x.InstructorId == model.InstructorId, ct)) ModelState.AddModelError(nameof(model.InstructorId), "Giảng viên không tồn tại.");
        if (await db.CourseClasses.AsNoTracking().AnyAsync(x => x.ClassCode == model.ClassCode && x.ClassId != currentId, ct)) ModelState.AddModelError(nameof(model.ClassCode), "Mã lớp đã tồn tại.");
    }

    private async Task<IActionResult> InvalidFormAsync(ClassFormViewModel model, CancellationToken ct) { await PopulateListsAsync(ct); return View("Form", model); }
    private async Task PopulateListsAsync(CancellationToken ct) { ViewBag.Courses = new SelectList(await db.Courses.AsNoTracking().OrderBy(x => x.CourseName).ToListAsync(ct), "CourseId", "CourseName"); ViewBag.Instructors = new SelectList(await db.Instructors.AsNoTracking().Include(x => x.User).OrderBy(x => x.User.FullName).ToListAsync(ct), "InstructorId", "User.FullName"); }
    private static ClassFormViewModel Map(CourseClass x) => new() { ClassId=x.ClassId,ClassCode=x.ClassCode,ClassName=x.ClassName,CourseId=x.CourseId,InstructorId=x.InstructorId,StartDate=x.StartDate,EndDate=x.EndDate,RegistrationOpenAt=x.RegistrationOpenAt,RegistrationCloseAt=x.RegistrationCloseAt,MaxCapacity=x.MaxCapacity,LearningMode=x.LearningMode,Location=x.Location,MeetingLink=x.MeetingLink,Status=x.Status,RequiresApproval=x.RequiresApproval,RowVersion=Convert.ToBase64String(x.RowVersion) };
    private static void Apply(ClassFormViewModel s, CourseClass t) { t.ClassCode=s.ClassCode;t.ClassName=s.ClassName;t.CourseId=s.CourseId;t.InstructorId=s.InstructorId;t.StartDate=s.StartDate;t.EndDate=s.EndDate;t.RegistrationOpenAt=s.RegistrationOpenAt;t.RegistrationCloseAt=s.RegistrationCloseAt;t.MaxCapacity=s.MaxCapacity;t.LearningMode=s.LearningMode;t.Location=s.Location?.Trim();t.MeetingLink=s.MeetingLink?.Trim();t.Status=s.Status;t.RequiresApproval=s.RequiresApproval; }
}
