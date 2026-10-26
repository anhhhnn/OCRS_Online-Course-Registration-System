using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCRS.Services;

namespace OCRS.Controllers;

[Authorize]
public sealed class NotificationsController(INotificationService notifications) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await notifications.GetMineAsync(userId, ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        await notifications.MarkReadAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, ct);
        return RedirectToAction(nameof(Index));
    }
}
