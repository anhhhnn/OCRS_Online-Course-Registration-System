using System.ComponentModel.DataAnnotations;
namespace OCRS.Models;
public class Category { public int CategoryId { get; set; } [Required, StringLength(100)] public string CategoryName { get; set; } = ""; [StringLength(500)] public string? Description { get; set; } public bool IsActive { get; set; } = true; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public ICollection<Course> Courses { get; set; } = []; }
