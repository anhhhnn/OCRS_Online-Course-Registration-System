using System.ComponentModel.DataAnnotations;
namespace OCRS.Models;
public class Student { public int StudentId { get; set; } [Required, StringLength(20)] public string StudentCode { get; set; } = ""; [Required] public string UserId { get; set; } = ""; public ApplicationUser User { get; set; } = null!; public ICollection<Enrollment> Enrollments { get; set; } = []; }
