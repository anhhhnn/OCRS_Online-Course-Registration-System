using OCRS.Models.Enums; using System.ComponentModel.DataAnnotations;
namespace OCRS.Models;
public class SyncLog { public int SyncLogId { get; set; } [Required,StringLength(100)] public string ExternalId { get; set; } = ""; public SyncStatus Status { get; set; } [StringLength(1000)] public string Message { get; set; } = ""; public DateTime SyncedAt { get; set; } = DateTime.UtcNow; }
