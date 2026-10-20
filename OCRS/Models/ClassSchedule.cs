using System.ComponentModel.DataAnnotations;
namespace OCRS.Models;
public class ClassSchedule { public int ScheduleId { get; set; } public int ClassId { get; set; } public CourseClass Class { get; set; } = null!; public DayOfWeek DayOfWeek { get; set; } public TimeOnly StartTime { get; set; } public TimeOnly EndTime { get; set; } [StringLength(300)] public string? RoomOrLink { get; set; } [StringLength(500)] public string? Note { get; set; } }
