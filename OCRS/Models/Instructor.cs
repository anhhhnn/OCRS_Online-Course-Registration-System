using System.ComponentModel.DataAnnotations;
namespace OCRS.Models;
public class Instructor { public int InstructorId { get; set; } [Required, StringLength(20)] public string InstructorCode { get; set; } = ""; [StringLength(200)] public string? Specialization { get; set; } [StringLength(1000)] public string? Biography { get; set; } [Required] public string UserId { get; set; } = ""; public ApplicationUser User { get; set; } = null!; public ICollection<CourseClass> Classes { get; set; } = []; }
