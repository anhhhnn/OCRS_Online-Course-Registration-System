using Microsoft.EntityFrameworkCore;
using OCRS.Data;
using OCRS.DTOs.SOA;
using OCRS.Models;
using OCRS.Models.Enums;

namespace OCRS.Services;

public interface INotificationService
{
    Task CreateAsync(string userId, string title, string content, NotificationType type = NotificationType.Information, int? relatedId = null, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationDto>> GetMineAsync(string userId, CancellationToken ct = default);
    Task<int> UnreadCountAsync(string userId, CancellationToken ct = default);
    Task<bool> MarkReadAsync(int notificationId, string userId, CancellationToken ct = default);
}

public sealed class NotificationService(ApplicationDbContext db) : INotificationService
{
    public async Task CreateAsync(string userId, string title, string content, NotificationType type = NotificationType.Information, int? relatedId = null, CancellationToken ct = default)
    {
        db.Notifications.Add(new Notification { UserId = userId, Title = title, Content = content, Type = type, RelatedEntityId = relatedId });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetMineAsync(string userId, CancellationToken ct = default) =>
        await db.Notifications.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationDto(x.NotificationId, x.Title, x.Content, x.Type.ToString(), x.IsRead, x.CreatedAt, x.RelatedEntityId))
            .ToListAsync(ct);

    public Task<int> UnreadCountAsync(string userId, CancellationToken ct = default) =>
        db.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, ct);

    public async Task<bool> MarkReadAsync(int notificationId, string userId, CancellationToken ct = default)
    {
        var changed = await db.Notifications.Where(x => x.NotificationId == notificationId && x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.IsRead, true), ct);
        if (changed > 0) return true;
        return await db.Notifications.AsNoTracking().AnyAsync(x => x.NotificationId == notificationId && x.UserId == userId, ct);
    }
}
