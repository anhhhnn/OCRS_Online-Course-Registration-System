using OCRS.Models.Enums; using System.ComponentModel.DataAnnotations;
namespace OCRS.Models;
public class Notification { public int NotificationId { get; set; } [Required] public string UserId { get; set; } = ""; public ApplicationUser User { get; set; } = null!; [Required,StringLength(160)] public string Title { get; set; } = ""; [Required,StringLength(1500)] public string Content { get; set; } = ""; public NotificationType Type { get; set; } public bool IsRead { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public int? RelatedEntityId { get; set; } }
