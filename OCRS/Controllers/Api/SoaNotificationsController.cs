using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCRS.DTOs.SOA;
using OCRS.Services;

namespace OCRS.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/soa/notifications")]
public sealed class SoaNotificationsController(INotificationService notifications) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> Mine(CancellationToken ct) => Ok(await notifications.GetMineAsync(UserId, ct));

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountDto>> UnreadCount(CancellationToken ct) => Ok(new UnreadCountDto(await notifications.UnreadCountAsync(UserId, ct)));

    [HttpPost("{id:int}/read")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ApiMessageDto>> Read(int id, CancellationToken ct) =>
        await notifications.MarkReadAsync(id, UserId, ct)
            ? Ok(new ApiMessageDto(true, "Đã đánh dấu thông báo là đã đọc."))
            : NotFound(new { message = "Không tìm thấy thông báo của tài khoản hiện tại." });
}
