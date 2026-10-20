using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
namespace OCRS.Models;
public class ApplicationUser : IdentityUser
{
    [Required, StringLength(120)] public string FullName { get; set; } = "";
    [DataType(DataType.Date)] public DateTime? DateOfBirth { get; set; }
    [StringLength(250)] public string? Address { get; set; }
    [StringLength(300)] public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Student? Student { get; set; }
    public Instructor? Instructor { get; set; }
    public ICollection<Notification> Notifications { get; set; } = [];
}
