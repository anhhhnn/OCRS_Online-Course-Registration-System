using System.ComponentModel.DataAnnotations;

namespace OCRS.DTOs.SOA;

public sealed record CurrentUserDto(
    string UserId,
    string FullName,
    string? Email,
    string? PhoneNumber,
    DateTime? DateOfBirth,
    string? Address,
    IReadOnlyCollection<string> Roles);

public sealed class UpdateCurrentUserDto
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = "";

    [Phone]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }
}
